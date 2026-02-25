using API大專.DTO;
using API大專.Models;
using API大專.service;
using API大專.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;


namespace API大專.Controllers
{
    [ApiController]
    [Route("charge")]
    [Authorize] // 直接放在 Class 上方，全 Controller 套用，不用重複寫
    public class EcpayController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        private readonly IConfiguration _config;
        //private readonly ILogger _logger;
        public EcpayController(ProxyContext proxyContext, IConfiguration config)//,ILogger<EcpayController> logger
        {
            _proxyContext = proxyContext;
            _config = config;            
        }

        [HttpPost("deposit")]
        public IActionResult CreateDeposit(decimal amount)
        {
            // 1️產生平台自己的訂單編號
            var orderNo = Guid.NewGuid().ToString("N").Substring(0, 20);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // 2️建立「尚未付款」的訂單
            var order = new DepositOrder
            {
                OrderNo = orderNo,
                UserId = userId,
                Amount = amount,
                Status = "PENDING"
            };

            // 3️先存起來
            _proxyContext.DepositOrders.Add(order);
            _proxyContext.SaveChanges();

            // 4️ 這一步之後才會碰綠界
            return Ok(orderNo);
        }


        private string GenerateEcpayForm(DepositOrder order)
        {
            // 1️ 從 appsettings環境設定 讀綠界設定
            var ecpay = _config.GetSection("ECPay");
            var tradeDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            // 2️ 組成綠界需要的參數
            var data = new Dictionary<string, string>
    {
        { "MerchantID", "3002607" },
        { "MerchantTradeNo", order.OrderNo },
        { "MerchantTradeDate", tradeDate },
        { "PaymentType", "aio" },//跟綠界說 我要用綠界的一站式金流
        { "TotalAmount", ((int)order.Amount).ToString() },//一定要是整數字串
        { "TradeDesc", "平台儲值"},//給綠界看的描述
        { "ItemName", "平台代幣儲值" },//給USER看 會顯示在付款畫面
        { "ReturnURL", "https://dampishly-interstellar-crista.ngrok-free.dev/charge/callback" },
        { "ChoosePayment", "Credit" },//付款方式 all=全開
        { "EncryptType", "1" } //使用SHA256 <-綠界接受的方式
    };

            // 3️ 計算檢查碼（防止資料被竄改）
            data["CheckMacValue"] = EcpayHelper.Generate(
                data,
                ecpay["HashKey"],
                ecpay["HashIV"]
            );

            // 4️回傳自動送出的 HTML 表單
            return EcpayHelper.BuildAutoSubmitForm(
                ecpay["ApiUrl"],
                data
            );
        }
        [AllowAnonymous]//允許不帶Token
        [HttpGet("pay/{orderNo}")]
        public IActionResult Pay(string orderNo)
        {
            var order = _proxyContext.DepositOrders.FirstOrDefault(o => o.OrderNo == orderNo);
            if (order == null) return NotFound();

            var html = GenerateEcpayForm(order);
            return Content(html, "text/html"); // 自動打開一個HTML畫面，回傳HTML 給綠界
        }
        [AllowAnonymous]//允許不帶Token
        [HttpPost("callback")]
        public IActionResult Callback([FromForm] Dictionary<string, string> data)
        {
            // 1防止有人假造 POST 資料到 callback endpoint，保護金流安全
            var ecpay = _config.GetSection("ECPay");
            if (!EcpayHelper.Verify(data, ecpay["HashKey"], ecpay["HashIV"]))
                return BadRequest("CheckMacValue invalid");

            //2 透過 MerchantTradeNo 找出平台內部訂單
            if (!data.TryGetValue("MerchantTradeNo", out var orderNo)) //透過回傳的TradeNo 找到對應orderNo 資料庫的訂單
                return BadRequest("Missing MerchantTradeNo");

            // 3️ 查找訂單
            var order = _proxyContext.DepositOrders
                .FirstOrDefault(o => o.OrderNo == orderNo);

            if (order == null)
                return NotFound("Order not found");

            // 4️ 防重送：如果已成功，直接回應綠界
            if (order.Status == "SUCCESS")
                return Content("1|OK"); // 綠界規定回傳 OK  反之 就是還沒接到這是第一次送

            // 5️ 根據綠界回傳狀態更新訂單
            var rtnCode = data.GetValueOrDefault("RtnCode"); // return 綠界回傳的固定代碼
                                                             // 1=成功，其他為失敗
            order.Status = (rtnCode == "1") ? "SUCCESS" : "FAILED";
            order.PaidAt = DateTime.Now;

            // 6️更新使用者錢包
            if (order.Status == "SUCCESS")
            {
                var user = _proxyContext.Users
                           .FirstOrDefault(u => u.Uid == order.UserId);
                if (user != null)
                {
                    // 取得異動前餘額
                    var before = user.Balance;

                    // 寫錢包紀錄
                    var log = new BalanceLog
                    {
                        UserId = user.Uid,
                        Action = "Deposit",  //deposit：儲值  spend：支出（下委託）  refund：退款    withdraw：提領 
                        Amount = order.Amount,
                        BeforeBalance = before,
                        AfterBalance = before + order.Amount, //order這次訂單廚的金額
                        RefType = "Deposit",  //儲值:deposit 對應refid :order_id     委託:對應commission的 commission_id  
                        RefId = order.DepositOrderId
                    };

                    // 更新使用者餘額
                    user.Balance += order.Amount;

                    // 7️儲存變更
                    _proxyContext.BalanceLogs.Add(log);
                }
            }

            _proxyContext.SaveChanges();

            // 8️回應綠界，表示已收到
            return Content("1|OK");
        }

    }
}
