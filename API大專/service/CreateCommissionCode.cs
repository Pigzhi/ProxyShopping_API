using API大專.Models;
using Microsoft.EntityFrameworkCore;

namespace API大專.service
{
    public class CreateCommissionCode
    {

        private readonly ProxyContext _ProxyContext;

        public CreateCommissionCode(ProxyContext proxycontext)
        {
            _ProxyContext = proxycontext;
        }

        public async Task<string> CreateCommissionCodeAsync(Commission commission)
        {

            var ym = DateTime.Now.ToString("yyyyMM");

            // 1. 取流水號（鎖）
            var seq = await _ProxyContext.Database
                .SqlQuery<int?>(
                    $"""
                SELECT seq As Value
                FROM commission_sequence WITH (UPDLOCK, HOLDLOCK)
                WHERE ym = {ym}
                """
                )
                .SingleOrDefaultAsync();

            if (seq == null)
            {
                seq = 1;
                await _ProxyContext.Database.ExecuteSqlRawAsync(
                    """
                INSERT INTO commission_sequence (ym, seq)
                VALUES ({0}, {1})
                """, ym, seq);
            }
            else
            {
                seq++;
                await _ProxyContext.Database.ExecuteSqlRawAsync(
                    """
                UPDATE commission_sequence
                SET seq = {0}, updated_at = SYSDATETIME()
                WHERE ym = {1}
                """, seq, ym);
            }

            // 2. 組 service_code
            var serviceCode = $"C-{ym}-{seq.Value:D6}";

            commission.ServiceCode = serviceCode;

            

            return serviceCode;



        }
    }
}
