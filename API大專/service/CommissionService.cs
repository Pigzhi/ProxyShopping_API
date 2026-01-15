using API大專.DTO;
using API大專.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using API大專.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API大專.service
{
    public class CommissionService
    {
        private readonly ProxyContext _ProxyContext;

        public CommissionService(ProxyContext proxycontext)
        {
            _ProxyContext = proxycontext;
        }

        public async Task<(bool success, string message)> EditCommissionAsync(int commissionId, string userId, CommissionEditDto dto)
        {
            using var transaction = await _ProxyContext.Database.BeginTransactionAsync();

            try
            {
                var Commission = await _ProxyContext.Commissions
                                        .FirstOrDefaultAsync(p => p.CommissionId == commissionId && p.CreatorId == userId); //驗證是不是訂單的建單人，跟委託是不是跟資料庫的是同一筆
                if (Commission == null)
                {
                    return (false, "找不到此委託");
                }

                if (Commission.Status != "審核中" && Commission.Status != "審核失敗")
                {
                    return (false, "此狀態不可編輯");
                }

                var user = await _ProxyContext.Users
                                   .FirstOrDefaultAsync(u => u.Uid == userId);
                if (user == null)
                    return (false, "使用者不存在");



                //判斷 修改內容 給歷史diff使用

                var oldDiff = new Dictionary<string, object>();
                var newDiff = new Dictionary<string, object>();

                //如果原本的委託的xx 不等於 Editdto送來的的xx 代表有更動
                if (Commission.Title != dto.Title)
                {
                    oldDiff["Title"] = Commission.Title;
                    newDiff["Title"] = dto.Title;
                }
                if (Commission.Description != dto.Description)
                {
                    oldDiff["Description"] = Commission.Description;
                    newDiff["Description"] = dto.Description;
                }
                if (Commission.Price != dto.Price)
                {//有更改才進來
                    oldDiff["Price"] = Commission.Price; //old設定成Com資料 舊的 
                    newDiff["Price"] = dto.Price;// new設定 dto新進來的資料
                }
                if (Commission.Quantity != dto.Quantity)
                {
                    oldDiff["Quantity "] = Commission.Quantity;
                    newDiff["Quantity "] = dto.Quantity;
                }
                if (Commission.Category != dto.Category)
                {
                    oldDiff["Category "] = Commission.Category;
                    newDiff["Category "] = dto.Category;
                }
                if (Commission.Deadline != dto.Deadline)
                {
                    oldDiff["Deadline "] = Commission.Deadline;
                    newDiff["Deadline "] = dto.Deadline.AddDays(7);
                }
                if (Commission.Location != dto.Location)
                {
                    oldDiff["Location "] = Commission.Location;
                    newDiff["Location "] = dto.Location;
                }

                var oldEscrow = Commission.EscrowAmount;

                //金額被修改 ->重算
                decimal feeRate = 0.1m;
                decimal newfee = (dto.Price * dto.Quantity) * feeRate; //新的手續費用
                decimal newtotal = Math.Round((dto.Price * dto.Quantity) + newfee
                                                    , 0, MidpointRounding.AwayFromZero);

                var diff = newtotal - oldEscrow; //金額差異
                if (diff > 0 && user.Balance < diff)
                {
                    return (false, "錢包餘額不足，金額變更失敗");
                }
                user.Balance -= diff;   //diff是多的就是 錢包-diff ， 改便宜 diff變-的 就是 錢包-(-diff);

                Commission.Title = dto.Title;
                Commission.Description = dto.Description;
                Commission.Price = dto.Price;
                Commission.Quantity = dto.Quantity;
                Commission.Category = dto.Category;
                Commission.Location = dto.Location;
                if (Commission.Deadline != dto.Deadline)
                {
                    Commission.Deadline = dto.Deadline.AddDays(7);
                } //在新的時間基礎上再往後加 7 天
                Commission.Fee = newfee;
                Commission.EscrowAmount = newtotal;

                Commission.Status = "審核中"; //編輯過都要重新審核

                //圖片處理
                if (dto.Image != null && dto.Image.Length > 0)
                {
                    var uploadPath = Path.Combine("wwwroot", "uploads");  // Path.Combine會依照 linux使用/或windows使用\
                    Directory.CreateDirectory(uploadPath);                                 //自動判斷有無資料夾 沒有就建

                    if (!string.IsNullOrEmpty(Commission.ImageUrl))
                    {
                        var oldImagePath = Path.Combine("wwwroot", Commission.ImageUrl.TrimStart('/')); //改成 wwwroot/uploads/xxx.jpg , 檔案系統不吃/開頭
                        if (File.Exists(oldImagePath)) //保護
                        {
                            File.Delete(oldImagePath);
                        }
                    }
                    //        Guid.NewGuid 產生不會重複的名字    path.GetExtension 會拿副檔名 （.jpg、.png）
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Image.FileName)}"; 
                    var filePath = Path.Combine(uploadPath, fileName);//實際上拿到後長這樣 wwwroot/uploads/3f7c9d3a-9f5c-4c7e-9b9a-21f4c3c1a8e2.png
                                                                      // FileMode.Create 沒檔案 → 建立；有檔案 → 覆蓋
                    using var stream = new FileStream(filePath, FileMode.Create); //new FileStream 在硬碟建立一個檔案 (filePath在這位置建立, )
                    await dto.Image.CopyToAsync(stream); //把 IFormFile 裡的資料，一口氣倒進剛剛開的水管

                    Commission.ImageUrl = $"/uploads/{fileName}";
                }


                //前面有 尋找 差異Diff  最後判斷哪邊有改 存進歷史
                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                if (oldDiff.Any())
                {
                    var history = new CommissionHistory
                    {
                        CommissionId = Commission.CommissionId,
                        Action = "EDIT",
                        ChangedBy = userId,
                        ChangedAt = DateTime.Now,
                        OldData = JsonSerializer.Serialize(oldDiff, jsonOptions),
                        NewData = JsonSerializer.Serialize(newDiff, jsonOptions)
                    };


                    _ProxyContext.CommissionHistories.Add(history);
                }




                await _ProxyContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "委託更改成功，狀態退回審核中");
            }
            catch
            {
                await transaction.RollbackAsync();
                return (false, "系統發生錯誤，請稍後再試");
            }
            //catch (Exception ex)
            //{
            //    await transaction.RollbackAsync();
            //    return 
            //    (
            //        false,
            //        ex.Message
                    
            //    );
            //}
        }
    }
}
