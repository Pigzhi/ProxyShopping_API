using API大專.DTO;
using API大專.Models;
using API大專.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


namespace API大專.service
{
    public class CommissionPaymentService
    {
        private readonly ProxyContext _proxycontext;
        private readonly IConfiguration _configuration;
        public CommissionPaymentService(ProxyContext proxycontext, IConfiguration configuration)
        {
            _proxycontext = proxycontext;
            _configuration = configuration;
        }

        /// Escrow → Seller（完成訂單）

        public async Task ReleaseToSellerAsync(int commissionId)
        {
            var commission = await _proxycontext.Commissions
                .FirstOrDefaultAsync(c => c.CommissionId == commissionId);

            if (commission == null)
                throw new Exception("委託不存在");

            if (commission.EscrowAmount <= 0)
                throw new Exception("Escrow 金額錯誤");

            var order = await _proxycontext.CommissionOrders
                .FirstOrDefaultAsync(o => o.CommissionId == commissionId);

            if (order == null)
                throw new Exception("訂單紀錄不存在");

            var seller = await _proxycontext.Users
                .FirstOrDefaultAsync(u => u.Uid == order.SellerId);

            if (seller == null)
                throw new Exception("找不到接委託人");

            var AdminUid = _configuration.GetSection("Platform:AdminUid").Value ;
            var Platform = await _proxycontext.Users
                .Where(c => c.Uid == AdminUid)
                .FirstOrDefaultAsync();
            if (Platform == null) {
                throw new Exception("系統發生錯誤"); }
            //定義撥款前錢包
            decimal? sellerBefore = seller.Balance;
            decimal? oldGMBalance = Platform.Balance;
            var escrowAmount = commission.EscrowAmount;
            var sellerIncome = escrowAmount - commission.Fee;
            var platformFee = commission.Fee;

            // 撥款
            seller.Balance += (commission.EscrowAmount - commission.Fee);
            Platform.Balance += commission.Fee;
            commission.EscrowAmount = 0;

            //Log錢包
            if (seller.Uid == order.SellerId)
            {
                var log = new BalanceLog
                {
                    UserId = seller.Uid, //跑腿方
                    Action = "Appropriation",  //deposit：儲值  spend：支出（下委託）  refund：退款    withdraw：提領 
                    Amount = (escrowAmount - commission.Fee),
                    BeforeBalance = sellerBefore,
                    AfterBalance = sellerBefore + sellerIncome,
                    RefType = "Commission",  //儲值:deposit 對應refid :order_id     委託:對應commission的 commission_id  
                    RefId = commission.CommissionId
                };
                _proxycontext.BalanceLogs.Add(log);
            }if (Platform.Uid == AdminUid) 
            {
                var log = new BalanceLog
                {
                    UserId = Platform.Uid, //跑腿方
                    Action = "GMFee",  //deposit：儲值  spend：支出（下委託）  refund：退款    withdraw：提領 
                    Amount = commission.Fee,
                    BeforeBalance = oldGMBalance,
                    AfterBalance = oldGMBalance + platformFee,
                    RefType = "Commission",  //儲值:deposit 對應refid :order_id     委託:對應commission的 commission_id  
                    RefId = commission.CommissionId
                };
                _proxycontext.BalanceLogs.Add(log);
            }
        
            
        }


        /// Escrow → Buyer（取消訂單）

        public async Task RefundToBuyerAsync(int commissionId)
        {
            var commission = await _proxycontext.Commissions
                .FirstOrDefaultAsync(c => c.CommissionId == commissionId);

            if (commission == null)
                throw new Exception("委託不存在");

            if (commission.EscrowAmount <= 0)
                throw new Exception("Escrow 金額錯誤");

            var buyer = await _proxycontext.Users
                .FirstOrDefaultAsync(u => u.Uid == commission.CreatorId);

            if (buyer == null)
                throw new Exception("找不到委託者");

            // 退款
            if (commission.Status == "COMPLETED")
                throw new Exception("訂單已完成，禁止重複撥款");
            buyer.Balance += commission.EscrowAmount;
            commission.EscrowAmount = 0;
            
        }
    }

}

