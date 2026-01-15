using API大專.DTO;
using API大專.Models;
using API大專.service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [Route("Commission")]
    public class CommissionProcessController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        private readonly CommissionService _CommissionService;
        private readonly CreateCommissionCode _CreateCode;
        public CommissionProcessController(ProxyContext proxyContext, CommissionService commissionService, CreateCommissionCode CreateCode)
        {
            _proxyContext = proxyContext;
            _CommissionService = commissionService;
            _CreateCode = CreateCode;
        }

        //新增委託 -> 錢包確認 扣款
        [HttpPost("Create")]
        public async Task<IActionResult> CreateCommission([FromForm] CommissionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState
                     .Where(x => x.Value.Errors.Count > 0)
                     .ToDictionary(k => k.Key, //欄位名稱
                     v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray() //value就是該欄位的狀態（包含 Errors）
                     )
                });
            }
            // 取得目前登入id
            var userid = "101";
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // 如果是 JWT / Session用這個
            var user = await _proxyContext.Users
    .FirstOrDefaultAsync(u => u.Uid == userid);


            if (user == null)
            {
                return Unauthorized(new { success = false, message = "請先登入！" });
            }
            //手續費 跟 總價 四捨五入
            decimal fee = 0.1m;
            decimal Pricefee = (dto.Price * dto.Quantity) * fee; //平台手續費
            decimal TotalPrice = Math.Round((dto.Price * dto.Quantity) + Pricefee,
                                                    0, MidpointRounding.AwayFromZero);

            //判斷錢包
            if (user.Balance < TotalPrice)
            {
                return BadRequest(new
                {
                    success = false,
                    code = "BALANCE_NOT_ENOUGH",
                    message = "錢包餘額不足"
                });
            }
            // 扣錢
            using var transaction = await _proxyContext.Database.BeginTransactionAsync();
            try
            {
                user.Balance -= TotalPrice;

                //圖片上傳
                string? imageUrl = null;
                string? FilePath = null;

                if (dto.Image != null && dto.Image.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Image.FileName)}";
                    FilePath = Path.Combine("wwwroot", "uploads", fileName);

                    imageUrl = $"/uploads/{fileName}";
                }

                var Commission = new Commission
                {
                    //creator_id = userId;            // 從 JWT / Session
                    //CreatorId = userid, // 抓session的id
                    CreatorId = userid,//測試
                    Title = dto.Title,
                    Description = dto.Description,
                    Price = dto.Price,
                    Fee = Pricefee, //平台手續費
                    Quantity = dto.Quantity,
                    Category = dto.Category,
                    Location = dto.Location,
                    EscrowAmount = TotalPrice, //Commission 委託 扣住金額
                    Deadline = dto.Deadline.AddDays(7), //結束日期自動加7天 還要審核

                    Status = "審核中",                                  // 預設
                    CreatedAt = DateTime.Now,              // 後端補
                    ImageUrl = imageUrl
                };
                var ServiceCode = await _CreateCode
                                 .CreateCommissionCodeAsync(Commission);

                _proxyContext.Commissions.Add(Commission);
                await _proxyContext.SaveChangesAsync();

                // 記錄歷史
                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var history = new CommissionHistory
                {
                    CommissionId = Commission.CommissionId,
                    Action = "CREATE",
                    ChangedBy = userid,
                    ChangedAt = DateTime.Now,
                    OldData = null,
                    NewData = JsonSerializer.Serialize(new
                    {
                        Commission.Title,
                        Commission.Description,
                        Commission.Price,
                        Commission.Quantity,
                        Commission.Category,
                        Commission.Deadline,
                        Commission.Location,
                    }, jsonOptions)
                };

                _proxyContext.CommissionHistories.Add(history);
                await _proxyContext.SaveChangesAsync();

                if (dto.Image != null && FilePath != null)  //DB 存檔後才上傳圖片， 不燃失敗圖片也依直傳..
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                    using var stream = new FileStream(FilePath, FileMode.Create);
                    await dto.Image.CopyToAsync(stream);
                }

                await transaction.CommitAsync();
                //回傳的資料
                var response = new CommissionDataDto
                {
                    ServiceCode = Commission.ServiceCode,
                    Title = Commission.Title,
                    Description = Commission.Description,
                    TotalPrice = TotalPrice,
                    Quantity = Commission.Quantity,
                    Category = Commission.Category,
                    Location = Commission.Location,
                    Status = Commission.Status,

                    CreatedAt = DateTime.Now,
                    Deadline = dto.Deadline.AddDays(7),
                    ImageUrl = Commission.ImageUrl
                };

                return Ok(new
                {
                    success = true,
                    data = response
                });
            }
            catch (Exception ex) //測試用
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
            //catch
            //{
            //    await transaction.RollbackAsync();

            //    return StatusCode(500, new
            //    {
            //        success = false,
            //        message = "建立委託失敗，請稍後再試"
            //    });
            //}
        }


        //編輯委託
        [HttpPut("{ServiceCode}/Edit")]
        public async Task<IActionResult> EditCommission(string ServiceCode, [FromForm] CommissionEditDto dto)
        {
            var commissionId = await _proxyContext.Commissions
                                                .Where(c => c.ServiceCode == ServiceCode)
                                                .Select(c => c.CommissionId)
                                                .FirstOrDefaultAsync();

            if (commissionId == 0)
                return NotFound("找不到委託");

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState
                                    .Where(x => x.Value.Errors.Count > 0)
                                    .ToDictionary(
                                    k => k.Key,
                                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                                )
                });
            }


            //id = 11; //模擬Commission id
            // 模擬user  之後要改session
            var userid = "101";

            var (success, message) = await _CommissionService
                                                         .EditCommissionAsync(commissionId, userid, dto);
            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = message
                });
            }

            return Ok(new
            {
                success = true,
                message = message
            });
        }

        //接受委託
        [HttpPost("{ServiceCode}/accept")]
        public async Task<IActionResult> acceptCommission(string ServiceCode)
        {
            var userid = "102";
            //var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            using var transaction = await _proxyContext.Database.BeginTransactionAsync();
            try
            {
                var commission = await _proxyContext.Commissions
                                .Where(c => c.ServiceCode == ServiceCode)
                                .Select(c => new
                                {
                                    c.CommissionId,
                                    c.CreatorId,
                                    c.EscrowAmount,
                                    c.Status
                                }).FirstOrDefaultAsync();

                if (commission == null || commission.Status != "待接單")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "訂單不存在或無法接單"
                    });
                }


                var affected = await _proxyContext.Database.ExecuteSqlRawAsync(@"
                          UPDATE Commission
                          SET Status = '已接單',
                          UpdatedAt = GETDATE()
                          WHERE 
                          commission_id = @id
                          AND status = '待接單'
                          AND creator_id <> @userId
                            ",
                    new SqlParameter("@id", commission.CommissionId),
                    new SqlParameter("@userId", userid)
                    );

                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        success = false,
                        message = "訂單已被接取或無法接單"
                    });
                }
                var newDiff = new Dictionary<string, object>();
                var oldDiff = new Dictionary<string, object>();
                var order = new CommissionOrder
                {
                    CommissionId = commission.CommissionId,
                    SellerId = userid,
                    BuyerId = commission.CreatorId,
                    Status = "PENDING", //未完成
                    Amount = commission.EscrowAmount,
                    CreatedAt = DateTime.Now
                };
                _proxyContext.CommissionOrders.Add(order);

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                newDiff["status"] = "已接單";
                oldDiff["status"] = "待接單";
                var history = new CommissionHistory
                {
                    CommissionId = commission.CommissionId,
                    Action = "ACCEPT",
                    ChangedBy = userid,
                    ChangedAt = DateTime.Now,
                    OldData = JsonSerializer.Serialize(oldDiff, jsonOptions),
                    NewData = JsonSerializer.Serialize(newDiff, jsonOptions)
                };


                _proxyContext.CommissionHistories.Add(history);


                await _proxyContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "訂單接受"
                });

            }
            //catch
            //{
            //    await transaction.RollbackAsync();
            //    return BadRequest(new
            //    {
            //        success = false,
            //        message = "接取訂單失敗，或是訂單已被接取"
            //    });
            //}
            catch (Exception ex) //如果報錯可以用
            {
                await transaction.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }


        }


        //上傳明細
        [HttpPost("{ServiceCode}/receipt")]
        public async Task<IActionResult> UploadReceipt(string ServiceCode, [FromForm] UploadReceiptDto dto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? "102";// 接單者
            var commissionId = await _proxyContext.Commissions
                                                 .Where(c => c.ServiceCode == ServiceCode)
                                                 .Select(c => c.CommissionId)
                                                 .FirstOrDefaultAsync();
            if (commissionId == 0)
            {
                return NotFound("委託不存在");
            }
            using var tx = await _proxyContext.Database.BeginTransactionAsync();

            var order = await _proxyContext.CommissionOrders
                .FirstOrDefaultAsync(o => o.CommissionId == commissionId && o.SellerId == userId);

            if (order == null)
                return Forbid("你不是接單者");

            var commission = await _proxyContext.Commissions
                .FirstOrDefaultAsync(c => c.CommissionId == commissionId);
            if (commission == null)
            {
                return NotFound("委託不存在");
            }
            if (dto.Image == null) { return BadRequest("請上傳圖片"); }

            if (commission.Status != "已接單" && commission.Status != "出貨中")
                return BadRequest("目前狀態不可上傳明細");


            var commissionReceipt = await _proxyContext.CommissionReceipts
                                   .FirstOrDefaultAsync(c => c.CommissionId == commissionId);
            bool isFirstUpload = commissionReceipt == null;
            var oldremark = commissionReceipt?.Remark;


            // 存圖片
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Image.FileName)}";
            var path = Path.Combine("wwwroot", "receipts", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var stream = new FileStream(path, FileMode.Create);
            await dto.Image.CopyToAsync(stream);

            var newImageUrl = $"/receipts/{fileName}";

            if (isFirstUpload)
            {
                commissionReceipt = new CommissionReceipt
                {
                    CommissionId = commissionId,
                    UploadedBy = userId
                };
                _proxyContext.CommissionReceipts.Add(commissionReceipt);
            }

            // 不管是不是第一次，都是更新「同一筆」
            commissionReceipt.ReceiptImageUrl = newImageUrl;
            commissionReceipt.ReceiptAmount = dto.ReceiptAmount;
            commissionReceipt.ReceiptDate = dto.ReceiptDate;
            commissionReceipt.Remark = dto.Remark;

            var oldStatus = commission.Status;
            if (commission.Status == "已接單")
            {
                commission.Status = "出貨中";
                commission.UpdatedAt = DateTime.Now;
            }


            var oldDiff = new Dictionary<string, object>();
            var newDiff = new Dictionary<string, object>();


            oldDiff["imageurl"] = (isFirstUpload == true ? "null" : commissionReceipt.ReceiptImageUrl);
            newDiff["imageurl"] = newImageUrl;

            //已接單在上面變為出貨中(第一次)，所以紀錄，後續再重傳都是出貨中所以只記錄圖片變化
            if (oldStatus != commission.Status)
            {
                oldDiff["status"] = oldStatus;
                newDiff["status"] = "出貨中";
            }
            if (oldremark != dto.Remark)
            {
                oldDiff["remark"] = oldremark;
                newDiff["remark"] = dto.Remark;
            }
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            if (oldDiff.Any())
            {
                _proxyContext.CommissionHistories.Add(new CommissionHistory
                {
                    CommissionId = commissionId,
                    Action = (oldStatus == "已接單" ? "UPLOAD_RECEIPT" : "REUPLOAD_RECEIPT"),
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    OldData = JsonSerializer.Serialize(oldDiff, jsonOptions),
                    NewData = JsonSerializer.Serialize(newDiff, jsonOptions)
                });
            }

            await _proxyContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, message = (oldStatus == "已接單" ? "明細上傳成功" : "明細重新上傳成功") });
        }


        //寄貨後按鈕
        [HttpPost("{ServiceCode}/ship")]
        public async Task<IActionResult> ShipCommission(string ServiceCode, [FromBody] CommissionShipDto dto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? "102"; // Swagger 測試用
            var commissionId = await _proxyContext.Commissions
                                                .Where(c => c.ServiceCode == ServiceCode)
                                                .Select(c => c.CommissionId)
                                                .FirstOrDefaultAsync();
            if (commissionId == 0)
            {
                return NotFound("委託不存在");
            }
            using var tx = await _proxyContext.Database.BeginTransactionAsync();

            // 1️ 驗證接單者
            var order = await _proxyContext.CommissionOrders
                .FirstOrDefaultAsync(o => o.CommissionId == commissionId && o.SellerId == userId);

            if (order == null)
                return Forbid("你不是接單者");

            // 2️ 驗證委託
            var commission = await _proxyContext.Commissions
                .FirstOrDefaultAsync(c => c.CommissionId == commissionId);

            if (commission == null)
                return NotFound("委託不存在");

            if (commission.Status != "出貨中" && commission.Status != "已寄出")
                return BadRequest("目前狀態不可更改");

            // 3️ 取得寄貨資料（只會有一筆）
            var shipping = await _proxyContext.CommissionShippings
                .FirstOrDefaultAsync(s => s.CommissionId == commissionId);

            var oldTrackingNumber = shipping?.TrackingNumber; //舊的nunber
            var oldLogistics = shipping?.LogisticsName;//舊的Name       
            var oldstatus = commission.Status; //出貨中
            var oldRemark = shipping?.Remark;

            bool isFirstShip = shipping == null;

            if (isFirstShip)
            {
                shipping = new CommissionShipping
                {
                    CommissionId = commissionId,
                    ShippedBy = userId,
                    Status = "已寄出"
                };
                _proxyContext.CommissionShippings.Add(shipping);
            }
            if (isFirstShip)
            {
                commission.Status = "已寄出";
                commission.UpdatedAt = DateTime.Now;
            }

            // 4️ 更新寄貨資訊
            shipping.Status = "已寄出";
            shipping.ShippedAt = DateTime.Now;
            shipping.LogisticsName = dto.LogisticsName;
            shipping.TrackingNumber = dto.TrackingNumber;
            shipping.Remark = dto.Remark;



            // 5History diff
            var oldDiff = new Dictionary<string, object>();
            var newDiff = new Dictionary<string, object>();

            oldDiff["shipping_status"] = isFirstShip ? "出貨中" : "已寄出";
            newDiff["shipping_status"] = "已寄出";
            if (oldLogistics != dto.LogisticsName)
            {
                oldDiff["logistics"] = oldLogistics;
                newDiff["logistics"] = dto.LogisticsName;
            }
            if (oldTrackingNumber != dto.TrackingNumber)
            {
                oldDiff["tracking_number"] = oldTrackingNumber;
                newDiff["tracking_number"] = dto.TrackingNumber;
            }
            if (oldstatus != shipping.Status)
            {
                oldDiff["commissionstatus"] = oldstatus; //出貨中
                newDiff["commissionstatus"] = shipping.Status;
            }
            if (oldRemark != dto.Remark)
            {
                oldDiff["remark"] = oldRemark;
                newDiff["remark"] = dto.Remark;
            }
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            if (oldDiff.Any())
            {
                _proxyContext.CommissionHistories.Add(new CommissionHistory
                {
                    CommissionId = commissionId,
                    Action = isFirstShip ? "SHIP_COMMISSION" : "RESHIP_COMMISSION",
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    OldData = JsonSerializer.Serialize(oldDiff, jsonOptions),
                    NewData = JsonSerializer.Serialize(newDiff, jsonOptions)
                });
            }
            await _proxyContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                success = true,
                message = isFirstShip ? "寄貨成功" : "寄貨資訊更新成功"
            });
        }

        //完成訂單 (買家)
        [HttpPost("{ServiceCode}/complete")]
        public async Task<IActionResult> CompleteCommission(string ServiceCode)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "101";

            using var tx = await _proxyContext.Database.BeginTransactionAsync();

            var commission = await _proxyContext.Commissions
                .FirstOrDefaultAsync(c => c.ServiceCode == ServiceCode);

            if (commission == null)
                return NotFound("委託不存在");

            if (commission.CreatorId != userId)
                return Forbid("你不是此委託的建立者");

            if (commission.Status != "已寄出")
                return BadRequest("目前狀態不可完成");

            var order = await _proxyContext.CommissionOrders
                .FirstOrDefaultAsync(o => o.CommissionId == commission.CommissionId);

            if (order == null || order.Status != "PENDING")
                return BadRequest("訂單紀錄不存在或訂單尚未完成寄貨");

            var oldStatus = commission.Status; //已寄出 狀態紀錄
            var paymentInfo = new
            {
                orderAmount = order.Amount,
                fee = commission.Fee,
                releaseToSeller = order.Amount - commission.Fee
            };

            // 狀態更新
            commission.Status = "已完成";
            commission.UpdatedAt = DateTime.Now;
            order.Status = "COMPLETED";
            order.FinishedAt = DateTime.Now;

            // 金流
            var paymentService = new CommissionPaymentService(_proxyContext);
            await paymentService.ReleaseToSellerAsync(commission.CommissionId);

            // History
            var oldDiff = new Dictionary<string, object>();
            var newDiff = new Dictionary<string, object>();
            if (oldStatus != commission.Status)
            {
                oldDiff["status"] = oldStatus;
                newDiff["status"] = commission.Status;
            }
            newDiff["payment"] = paymentInfo;



            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            if (oldDiff.Any())
            {
                _proxyContext.CommissionHistories.Add(new CommissionHistory
                {
                    CommissionId = commission.CommissionId,
                    Action = "COMPLETE_COMMISSION",
                    ChangedBy = userId,
                    OldData = JsonSerializer.Serialize(oldDiff, jsonOptions),
                    NewData = JsonSerializer.Serialize(newDiff, jsonOptions)
                });
            }
            await _proxyContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, message = "訂單已完成" });
        }


        //商品瑕疵 取消 委託人必須退貨給 接委託人(承擔成本)
        [HttpPost("{ServiceCode}/cancel")]
        public async Task<IActionResult> CancelCommission(string ServiceCode, [FromBody] CommissionCancelDto dto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "101";

            using var tx = await _proxyContext.Database.BeginTransactionAsync();

            var commission = await _proxyContext.Commissions
                .FirstOrDefaultAsync(c => c.ServiceCode == ServiceCode);

            if (commission == null)
                return NotFound("委託不存在");

            if (commission.CreatorId != userId)
                return Forbid("系統錯誤，你不是此委託者");

            if (commission.Status != "已寄出")
                return BadRequest("目前狀態不可取消");

            var order = await _proxyContext.CommissionOrders
                .FirstOrDefaultAsync(o => o.CommissionId == commission.CommissionId);

            if (order == null || order.Status != "PENDING")
                return BadRequest("訂單紀錄不存在");

            var oldStatus = commission.Status; //紀錄舊狀態
            var cancelInfo = new
            {
                Amount = commission.EscrowAmount,
                to = commission.Creator,
            };


            // 狀態
            commission.Status = "cancelled";
            commission.UpdatedAt = DateTime.Now;
            order.Status = "CANCELLED";
            order.FinishedAt = DateTime.Now;

            // 退款
            var paymentService = new CommissionPaymentService(_proxyContext);
            await paymentService.RefundToBuyerAsync(commission.CommissionId);

            // History
            var oldDiff = new Dictionary<string, object>();
            var newDiff = new Dictionary<string, object>();

            if (oldStatus != "cancelled" && oldStatus == "已寄出")
            {
                oldDiff["status"] = oldStatus;
                newDiff["status"] = commission.Status;
            }
            if (dto.Reason != null)
            {
                oldDiff["Reason"] = null;
                newDiff["Reason"] = dto.Reason;
            }
            newDiff["cancelAmount"] = cancelInfo;

            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            _proxyContext.CommissionHistories.Add(new CommissionHistory
            {
                CommissionId = commission.CommissionId,
                Action = "CANCEL_COMMISSION",
                ChangedBy = userId,
                OldData = JsonSerializer.Serialize(oldDiff, jsonOptions),
                NewData = JsonSerializer.Serialize(newDiff, jsonOptions)
            });

            await _proxyContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, message = "訂單已取消並退款" });
        }

    }
}
