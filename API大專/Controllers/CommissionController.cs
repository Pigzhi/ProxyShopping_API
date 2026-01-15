using API大專.DTO;
using API大專.Models;
using API大專.service;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API大專.Controllers
{
    [ApiController]
    [Route("api/Commissions")]
    public class CommissionController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        private readonly CommissionService _CommissionService;
        private readonly CreateCommissionCode _CreateCode;
        public CommissionController(ProxyContext proxyContext, CommissionService commissionService, CreateCommissionCode CreateCode)
        {
            _proxyContext = proxyContext;
            _CommissionService = commissionService;
            _CreateCode = CreateCode;
        }

        //委託 待接單所有 展示
        [HttpGet]
        public async Task<IActionResult> GetCommissionsList() 
        { 
            var commissions = await _proxyContext.Commissions
                                                .Where(u=>u.Status == "待接單")
                                                .OrderByDescending(u=>u.UpdatedAt) //由大到小時間抓
                                                .Select(u=> new 
                                                { 
                                                u.ServiceCode,
                                                u.Title,
                                                u.Price,
                                                u.Quantity,
                                                u.Location,
                                                u.Category,
                                                u.ImageUrl,
                                                u.Deadline,
                                                u.Status
                                                }).ToListAsync();

            return Ok(new
            {
                success = true,
                data = commissions
            });
        }

        //點擊委託之後 顯示的單筆詳細資料
        [HttpGet("{ServiceCode}")]
        public async Task<IActionResult> GetDetail(string ServiceCode) 
        {
            var Commission = await _proxyContext.Commissions
                                               .Where(c => c.ServiceCode == ServiceCode && c.Status == "待接單")
                                               .Select(c => new
                                               {    //比普通清單多
                                                   c.ServiceCode,
                                                   c.Title,
                                                   c.Description, //描述
                                                   c.Price, 
                                                   c.Quantity,
                                                   c.Fee,                //平台手續費
                                                   c.EscrowAmount, // 會拿到的總價格
                                                   c.Category,
                                                   c.Location,
                                                   c.ImageUrl,
                                                   c.CreatedAt, //這委託建立的時間
                                                   c.Deadline,
                                                   c.Status        //顯示用
                                               }).FirstOrDefaultAsync();
            if (Commission==null) {
                return NotFound(
                            new
                            {
                                success = false,
                                message = "找不到此委託"
                            } );
            }
            return Ok(new
            {
                success = true,
                data = Commission
            });
        
        }

        


        

    }
}
