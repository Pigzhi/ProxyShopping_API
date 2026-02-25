using API大專.DTO;
using API大專.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;


namespace API大專.service
{
    public class ReviewService
    {
        private readonly ProxyContext _proxycontext;

        public ReviewService(ProxyContext proxycontext)
        {
            _proxycontext = proxycontext;
        }
        public async Task<IEnumerable<ReviewSearchDto>> GetReviewsByTargetCode(string targetType, string targetCode)
        {
            var commission = await _proxycontext.Commissions
                                              .FirstOrDefaultAsync(c => c.ServiceCode == targetCode);
            if (commission == null) {
                return new List<ReviewSearchDto>();
            }
            return await _proxycontext.Reviews
                .Where(r => r.TargetType == targetType && r.TargetId == commission.CommissionId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewSearchDto
                {
                    ReviewId = r.ReviewId,
                    ReviewerUid = r.ReviewerUid,
                    Result = r.Result,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }


        public async Task Review(ReviewRequestDto req, string reviewerUid)
        {
            using var tx = await _proxycontext.Database.BeginTransactionAsync();

            var commission = await _proxycontext.Commissions.FirstOrDefaultAsync(c => c.ServiceCode == req.TargetCode);
            if (req.TargetType == "commission")
            {
                if (commission == null)
                    throw new Exception("Commission not found");
                if (commission.Status != "審核中")
                    throw new InvalidOperationException("此委託單目前不可審核");
            }
            //else if (req.TargetType == "product")
            //{               
            //    targetId = int.Parse(req.TargetId); // 假設 product 用 id
            //}
            else
            {
                throw new Exception("Invalid target_type");
            }
            try
            {
                _proxycontext.Reviews.Add(new Review
                {
                    TargetType = req.TargetType,
                    TargetId = commission.CommissionId,      // int
                    //TargetCode = commission.ServiceCode,
                    ReviewerUid = reviewerUid,
                    Result = req.Result,
                    Reason = req.Reason,
                    CreatedAt = DateTime.Now
                });

                // 2. 若失敗，更新對應業務資料
                if (req.Result == 0)
                {
                    await HandleFail(req.TargetType, commission.CommissionId);
                }
                else
                {
                    await HandlePass(req.TargetType, commission.CommissionId);
                }
                await _proxycontext.SaveChangesAsync();
                await tx.CommitAsync();
            } 
            catch
            {
                await tx.RollbackAsync();
                throw; // 讓外層 Controller 拿到 Exception
            } 
        }
        //大於五次FAIL
        private async Task HandleFail(string targetType, int targetId)
        {
            const int MAX_FAIL = 5;

            //if (targetType == "product")
            //{
            //var product = _proxycontext.Products.Find(targetId)
            //    ?? throw new Exception("Product not found");

            //if (product.FailCount > MAX_FAIL)
            //    product.DisabledUntil = DateTime.Now.AddDays(7);
            //    throw new KeyNotFoundException("還沒有商品");
            //}
            if (targetType != "commission")
            {
                throw new Exception("Invalid target_type");
            }
            var commission = await _proxycontext.Commissions
                                              .Include(c => c.Creator) // 一定要拿到 user
                                              .FirstOrDefaultAsync(c => c.CommissionId == targetId)
                                              ?? throw new Exception("Commission not found");


            commission.Status = "審核失敗";
            commission.UpdatedAt = DateTime.Now;
            var creatorId = commission.CreatorId;

            var BlackReTime = DateTime.Now.AddDays(-30);
            var FailCount = await _proxycontext.Reviews
                                         .Where(r => r.TargetType == "commission" && r.Result == 0 && r.CreatedAt >= BlackReTime)
                                         .Join(_proxycontext.Commissions, r => r.TargetId, c => c.CommissionId, (r, c) => c.CreatorId) //join( 要join的集合 , 外層的key , 內層的key , Lambda 的參數列表，對應 Join 的兩個集合然後要輸出的東西creatorid)
                                         .CountAsync(id =>id == creatorId); //看這些委託單是不是這個使用者的
            //在最近 30 天內，這個使用者創建的 Commission，失敗的審核有多少次    
            if (FailCount >= MAX_FAIL)
            {
                var user = await _proxycontext.Users
                    .FirstAsync(u => u.Uid == creatorId);

                user.DisabledUntil = DateTime.Now.AddDays(7);
            }


        }
        private async Task HandlePass(string targetType, int targetId)
        {
            if (targetType == "commission")
            {
                var commission = await _proxycontext.Commissions.FindAsync(targetId)
                    ?? throw new Exception("Commission not found");

                commission.Status = "待接單";
                commission.UpdatedAt = DateTime.Now;
            }
        }
        public async Task<IEnumerable<CommissionReviewDto>> GetPendingCommissionsForReview()
        {
            // 只抓審核中
            var commissions =await  _proxycontext.Commissions
                                   .Where(c => c.Status == "審核中")
                                   .OrderBy(c => c.CreatedAt)
                                   .ToListAsync();
            var commissionIds = commissions
                              .Select(c => c.CommissionId).ToList();//對記憶體裡的集合做操作
            var reviewList = await _proxycontext.Reviews
                                          .Where(r => r.TargetType == "commission" 
                                          && commissionIds.Contains(r.TargetId)//挑出 TargetId 在 commissionIds 這個清單裡的 review
                                          && r.Result == 0)
                                          .ToListAsync();
            var result = commissions.Select(c =>
            {
                // 找該 commission 的失敗 review
                var failReviews = reviewList
                    .Where(r => r.TargetId == c.CommissionId && r.Result == 0)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

                return new CommissionReviewDto
                {
                    ServiceCode = c.ServiceCode,
                    Title = c.Title,
                    CreatorId = c.CreatorId,
                    ImageUrl = c.ImageUrl,
                    Description = c.Description,
                    Price = c.Price,
                    Quantity = c.Quantity,
                    Category = c.Category,
                    Location = c.Location,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    Deadline = c.Deadline,
                    EscrowAmount = c.EscrowAmount,                  
                    LatestFailReason = failReviews.FirstOrDefault()?.Reason // 只取最新一筆失敗原因
                };
            }).ToList();

            return result;
        }


    }
}
