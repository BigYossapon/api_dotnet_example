using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using userstrctureapi.Data;
using userstrctureapi.Services;

var builder = WebApplication.CreateBuilder(args);

// 📌 อ่านค่า Connection String จาก appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionStringpg = builder.Configuration.GetConnectionString("PostgresConnection");
// 📌 ลงทะเบียน DbContext ให้ใช้ MySQL
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<AuditInterceptor>();
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseNpgsql(connectionStringpg));

// builder.Services.AddHttpContextAccessor(); // ✅ ต้องมีบรรทัดนี้!
// builder.Services.AddScoped<AuditInterceptor>();

// ➤ 3. ลงทะเบียน `AppDbContext` สำหรับ MySQL พร้อม Interceptor
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
               .AddInterceptors(interceptor);

});

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

// FirebaseApp.Create(new AppOptions()
// {
//     Credential = GoogleCredential.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mauifirebasedemo-firebase-adminsdk.json")),
// });
var app = builder.Build();



app.UseMiddleware<AuditMiddleware>();
// using (var scope = app.Services.CreateScope())
// {
//     var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
//     // auditDb.Database.EnsureCreated(); // สร้าง Table อัตโนมัติ
// }

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
