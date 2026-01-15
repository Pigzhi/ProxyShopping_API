using API大專.DTO;
using API大專.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;


namespace API大專.Controllers
{
    [ApiController]
    [Route("api/admin/History")]
    public class HistoryCommissionController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        public HistoryCommissionController(ProxyContext proxyContext)
        {
            _proxyContext = proxyContext;
        }
        [HttpGet]
        public async Task<IActionResult> SearchHistoryALL()
        {
            var userid = "administrator";
            var user = await _proxyContext.Users
                       .FirstOrDefaultAsync(c => c.Name == userid);
            if (user == null && userid != "administrator") 
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "尚未登入，或是權限不足"
                });
            }

            var History = await _proxyContext.CommissionHistories
                          .OrderBy(c => c.CommissionId)
                          .Select(c => new
                          {
                              historyid=c.HistoryId,
                              commissionid=c.CommissionId,
                              action=c.Action,
                              changedby=c.ChangedBy,
                              changedAt=c.ChangedAt,
                              oldData=c.OldData,
                              newData=c.NewData,
                          }).ToListAsync();
            return Ok(
                new { 
                success=true,
                data= History
                });
        }
        [HttpGet("{ServiceCode}")]
        public async Task<IActionResult> SearchHistoryOnly(String ServiceCode)
        {
            var userid = "administrator";
            var user = await _proxyContext.Users
                       .FirstOrDefaultAsync(c => c.Name == userid);
            if (user == null && userid != "administrator")
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "尚未登入，或是權限不足"
                });
            }
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

    }
}
