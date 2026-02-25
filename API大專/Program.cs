using API大專.Models;
using API大專.service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("ProxyContext");
builder.Services.AddDbContext<ProxyContext>(x => x.UseSqlServer(connectionString));

//Swagger使用
builder.Services.AddSwaggerGen(options =>
{
    // 定義安全設定
    options.AddSecurityDefinition("Pig", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header, //生出來的Token放到Header後面
        Description = "請輸入 Bearer [空格] 加上Token。例如：Bearer abc123def"
    });

    // 讓所有 API 預設都要套用這個安全需求
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Pig" }
            },
            new string[] {}
        }
    });
});

//註冊JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("Jwt:Key").Value!)),    // 使用這把 Key 來解碼檢查
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetSection("Jwt:Issuer").Value,
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetSection("Jwt:Audience").Value,
            ValidateLifetime = true,                //4. 驗證過期時間
            ClockSkew = TimeSpan.Zero,      // 5. 緩衝時間設為 0 (過期zero就是一秒即刻失效)
        };
    });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();


builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<CommissionService>();
builder.Services.AddScoped<CreateCommissionCode>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvc",
        policy =>
        {
            policy.WithOrigins("https://localhost:5032")

                      .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowMvc");
//app.UseHttpsRedirection();
app.UseAuthentication(); // 1. 認證：你是誰？ (檢查 Token 是否合法)
app.UseAuthorization();  // 2. 授權：你能做什麼？ (檢查你有無 Role 或 Policy)


app.UseAuthorization();

app.MapControllers();

app.Run();
