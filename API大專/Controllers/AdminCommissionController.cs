using API大專.DTO;
using API大專.Models;
using API大專.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;


namespace API大專.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminCommissionController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        private readonly ReviewService _RVservice;
        public AdminCommissionController(ProxyContext proxyContext,ReviewService RVservice)
        {
            _proxyContext = proxyContext;
            _RVservice = RVservice;
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet("History")]
        public async Task<IActionResult> SearchHistoryALL()
        {
            

            var History = await _proxyContext.CommissionHistories
                          .OrderBy(c => c.CommissionId)
                          .Select(c => new
                          {
                              historyid = c.HistoryId,
                              commissionid = c.CommissionId,
                              action = c.Action,
                              changedby = c.ChangedBy,
                              changedAt = c.ChangedAt,
                              oldData = c.OldData,
                              newData = c.NewData,
                          }).ToListAsync();
            return Ok(
                new
                {
                    success = true,
                    data = History
                });
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet("History/{ServiceCode}")]
        public async Task<IActionResult> SearchHistoryOnly(String ServiceCode)
        {
            
            
            var commissionid = await _proxyContext.Commissions
                    .Where(c => c.ServiceCode == ServiceCode)
                    .Select(c => c.CommissionId)
                    .FirstOrDefaultAsync();
            if (commissionid == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "找不到委託"
                });
            }
            var History = await _proxyContext.CommissionHistories
                         .Where(c => c.CommissionId == commissionid)
                         .OrderBy(c => c.ChangedAt)
                         .Select(c => new
                         {
                             historyid = c.HistoryId,
                             commissionid = c.CommissionId,
                             action = c.Action,
                             changedby = c.ChangedBy,
                             changedAt = c.ChangedAt,
                             oldData = c.OldData,
                             newData = c.NewData,
                         }).ToListAsync();
            return Ok(
                new
                {
                    success = true,
                    data = History
                });

        }

        // 依照{commission/product} 查詢 {流水號} 找審核紀錄
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{targetType}/{TargetCode}")]
        public async Task<IActionResult> Get(string targetType, string TargetCode)
        {
            var result = await _RVservice.GetReviewsByTargetCode(targetType, TargetCode);
            return Ok(result);
        }

        //撈審核清單
        [Authorize(Roles = "ADMIN")]
        [HttpGet("Review/Pending")]
        public async Task<IActionResult> GetPending()
        {
            return Ok(await _RVservice.GetPendingCommissionsForReview());
        }

        // 審核
        [Authorize(Roles = "ADMIN")]
        [HttpPost("Review/Pending")]
        public async Task<IActionResult> Review([FromBody] ReviewRequestDto req)
        {
            var reviewerUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reviewerUid))
            { 
                return Unauthorized("找不到審核者資訊"); 
            }
            
            await _RVservice.Review(req, reviewerUid);
            return Ok();
        }

    }
}
