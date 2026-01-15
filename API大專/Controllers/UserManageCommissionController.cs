using Microsoft.AspNetCore.Mvc;
using API大專.DTO;
using API大專.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
namespace API大專.Controllers
{
    [ApiController]
    [Route("api/Manage")]
    public class UserManageCommissionController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        public UserManageCommissionController(ProxyContext proxyContext)
        {
            _proxyContext = proxyContext;
        }
        [HttpGet("Commission")]
        public async Task<IActionResult> UserManage(string userid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? "101";// 接單者
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("尚未登入");
            }
            var commissions = await _proxyContext.Commissions
                            .Where(c => c.CreatorId == userId && c.Status != "已完成" && c.Status != "cancelled")
                            .Select(c => new UserCommissionManageDto
                            {
                                ServiceCode = c.ServiceCode,
                                Title = c.Title,
                                Status = c.Status,
                                Quantity = c.Quantity,
                                Price = c.Price,
                                TotalAmount = (c.Price * c.Quantity )+ c.Fee,
                                CreatedAt = c.CreatedAt,
                                EndAt = c.Deadline,
                                ImageUrl = c.ImageUrl,

                                CanEdit = c.Status == "審核中" || c.Status=="審核失敗",
                                CanViewDetail = c.Status == "已出貨",
                                CanViewShipping = c.Status == "已寄出"
                            }).ToListAsync();

            return Ok(commissions);





        }

            [HttpGet("Commission/MyAceipt")]
            public async Task<IActionResult> AceiptManage()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? "102";// 接單者
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("尚未登入");
                }
           
                var order = await (
                            from o in _proxyContext.CommissionOrders
                            join p in _proxyContext.Commissions
                            on o.CommissionId equals p.CommissionId
                            where o.Status == "PENDING" && o.SellerId == userId
                            select new AceiptCommissionManageDto
                            {
                                ServiceCode = p.ServiceCode,
                                Title = p.Title,
                                Status = p.Status,
                                Quantity = p.Quantity,
                                Price = p.Price,
                                TotalAmount = (p.Price * p.Quantity) + p.Fee,
                                PlatformFee = p.Fee,
                                CreatedAt = o.CreatedAt,
                                ImageUrl = p.ImageUrl,

                                CanUpdateReceipt = o.Status == "PENDING",
                                CanUpdateShip = o.Status == "PENDING" && p.Status =="出貨中",
                                CanViewReceipt = p.Status == "出貨中",//可以看自己上船的明細
                                CanViewShipping = p.Status == "已寄出"//可以看自己上傳的寄貨資訊
                            }
                            ).ToListAsync();

                return Ok(order);





            }

    }
}
