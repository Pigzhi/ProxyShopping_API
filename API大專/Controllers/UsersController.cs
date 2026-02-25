using API大專.DTO;
using API大專.Models;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API大專.Controllers
{
    [ApiController]
    [Route("Register")]
    public class UsersController : ControllerBase
    {
        private readonly ProxyContext _proxyContext;
        private readonly IConfiguration _configuration;
        public UsersController(ProxyContext proxyContext, IConfiguration configuration)
        {
            _proxyContext = proxyContext;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (await _proxyContext.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("該 Email 已經被註冊過囉！");
            }

            string Role = "USER"; // 預設身分
            var secretCode = _configuration.GetSection("AdminSettings:InviteCode").Value;


            // 如果輸入了正確的秘密邀請碼
            if (request.InviteCode == secretCode)
            {
                Role = "ADMIN";
            }

            //  使用 BCrypt 對密碼進行加密 (不可逆雜湊)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Uid = Guid.NewGuid().ToString(),
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Phone = request.Phone,
                identity = Role,   //正常是user，但邀請馬正確會變為admin
                address = request.Address,
                CreatedAt = DateTime.Now
            };


            _proxyContext.Users.Add(user);
            await _proxyContext.SaveChangesAsync();

            return Ok(new { message = "註冊成功！" });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            // 1. 尋找使用者
            var user = await _proxyContext.Users.FirstOrDefaultAsync(u => u.Name == request.Name); //對比資料庫
            if (user == null)
            {
                return BadRequest("帳號或密碼錯誤");
            }

            // 2. 比對加密密碼
            // 使用 BCrypt 驗證前端傳來的明文密碼與資料庫裡的 Hash 是否一致  
            // BCrypt 會提取資料庫 Hash 裡的鹽值(salt)]，將使用者剛剛輸入的密碼重新雜湊，比對結果是否一致。
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("帳號或密碼錯誤");
            }

            // 3. 密碼正確，發放 JWT Token
            string token = CreateToken(user);

            // 回傳 Token 以及一些基本資訊給前端
            return Ok(new
            {
                token = token,
                name = user.Name,
                email = user.Email,
                balance = user.Balance,
                userId = user.Uid,
            });
        }

        private string CreateToken(User user)
        {
            // 設定「聲明 (Claims)」：這是 Token 裡面攜帶的資訊
            // 這個聲明是一個陣列
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Uid) ,// 將 Uid 存入 Token
            new Claim(ClaimTypes.Role, user.identity)
        };
            // 從 appsettings.json 讀取密鑰
            // SymmetricSecurityKey 是JWT 對稱加密必須使用的容器。
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration.GetSection("Jwt:Key").Value!));
            // Encoding.UTF8.GetBytes -> 翻譯成電腦懂的樣子
            // SymmetricSecurityKey -> 對稱金鑰,加密解密都用同一個鑰匙
            // Value! -> 保證不是null

            // 設定簽署憑證（使用 HmacSha512 演算法）
            // 告訴系統：「我要用這把鑰匙，搭配 Sha512 這台演算法壓模機來簽署 Token」。
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            // 建立 Token 內容
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration.GetSection("Jwt:Issuer").Value,              // 填入 JWT設定好的Issuer的值
                audience: _configuration.GetSection("Jwt:Audience").Value,
                claims: claims,                        //claims 決定了 Payload內容
                expires: DateTime.Now.AddDays(1),              // Token 有效期為 1 天 ，時間到就作廢
                signingCredentials: creds                  //->  (SigningCredentials)包含了key(一串亂碼，跟HmacSha512演算)
            );                             //signingCredentials 則決定了 Signature (簽章)

            // 產出字串形式的 JWT
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }


    }
}
