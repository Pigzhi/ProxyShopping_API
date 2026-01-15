using API大專.DTO;
using API大專.Models;
using API大專.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using System.Security.Claims;
using System.Text.Json;

namespace API大專.Controllers
{
    [ApiController]
    [Route("api/Commissions")]
    public class CompletedCommission : Controller
    {
        private readonly ProxyContext _proxyContext;
        public CompletedCommission(ProxyContext proxyContext)
        {
            _proxyContext = proxyContext;
        }

        // 登入的user自己 建立的委託 完成 跟 接受委託 完成 都顯示
        //[Authorize]
        [HttpGet("MyCompleted")]
        public async Task<ActionResult> GetCompletedOrder([FromQuery] string userId)
        {
            // var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "請提供使用者身分"
                });
            }
            var Orders = await (
                        from o in _proxyContext.CommissionOrders
                        join c in _proxyContext.Commissions
                        on o.CommissionId equals c.CommissionId
                        join buyerUser in _proxyContext.Users
                        on o.BuyerId equals buyerUser.Uid
                        join sellerUser in _proxyContext.Users
                        on o.SellerId equals sellerUser.Uid
                        where o.Status == "COMPLETED" || o.Status == "CANCELLED"
                        && (o.BuyerId == userId || o.SellerId ==userId)
                        select new
                        {
                            title = c.Title,
                            imageurl = c.ImageUrl,
                            description = c.Description,
                            location = c.Location,
                            status = o.Status,
                            amount = o.Amount,
                            finishedAt = o.FinishedAt,
                            buyer = new
                            {
                                id = buyerUser.Uid,
                                name = buyerUser.Name
                            },
                            seller = new
                            {
                                id = sellerUser.Uid,
                                name = sellerUser.Name
                            }
                        }
                            ).ToListAsync();
            return Ok(new
            {
                success = true,
                data = Orders
            });

        }

        



    }
}
