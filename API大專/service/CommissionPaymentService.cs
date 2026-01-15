using API大專.DTO;
using API大專.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using API大專.Validation;
using Microsoft.AspNetCore.Http.HttpResults;


namespace API大專.service
{
    public class CommissionPaymentService
    {
        private readonly ProxyContext _proxycontext;

        public CommissionPaymentService(ProxyContext proxycontext)
        {
            _proxycontext = proxycontext;
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

            // 撥款
            seller.Balance += (commission.EscrowAmount-commission.Fee); 
            commission.EscrowAmount = 0;
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

