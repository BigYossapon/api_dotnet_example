using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using userstrctureapi.Data;
using userstrctureapi.Services;

var builder = WebApplication.CreateBuilder(args);

// 📌 อ่านค่า Connection String จาก appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 📌 ลงทะเบียน DbContext ให้ใช้ MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// 📌 ลงทะเบียน AuthService
builder.Services.AddScoped<AuthService>();

// 📌 ลงทะเบียน Authentication และ JWT Bearer Token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("aaa")), // รหัสลับที่ใช้ในการเซ็น JWT
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 📌 เปิดใช้งาน Controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer(); // เพิ่มการค้นหา endpoints
builder.Services.AddSwaggerGen();


// 📌 ใช้งาน CORS (ถ้าต้องการให้รองรับการเรียก API จากที่อื่น)
// builder.Services.AddCors(policy =>
//     policy.AllowAnyOrigin()
//           .AllowAnyMethod()
//           .AllowAnyHeader()
// );

// 📌 ใช้ Authentication และ Authorization
var app = builder.Build();

if (app.Environment.IsDevelopment()) // ใช้เฉพาะใน Development
{
    app.UseSwagger(); // เปิดการใช้ Swagger
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"); // กำหนด endpoint ของ Swagger
        c.RoutePrefix = string.Empty; // ตั้งให้เปิด Swagger UI ที่ root path (http://localhost:5000)
    });
}
app.UseCors(); // ใช้ CORS ที่ลงทะเบียนไว้
app.UseAuthentication(); // ใช้การตรวจสอบ JWT
app.UseAuthorization(); // ใช้การอนุญาตเข้าถึง API

app.MapControllers();

app.Run();
