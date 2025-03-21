using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using userstrctureapi.Models;
using userstrctureapi.Data;
using MySql.Data.MySqlClient;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace userstrctureapi.Services
{
    public class AuthService
    {
        private readonly string _connectionString;
        private readonly AppDbContext _db;
        private readonly AuditDbContext _auditDb;
        // private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        private readonly IHubContext<RealTimeHub> _hubContext;

        public AuthService(IHubContext<RealTimeHub> realTimeHub, AuditDbContext auditDb, AppDbContext db, IConfiguration config)
        {
            _db = db;
            _auditDb = auditDb;
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection")!;
            _hubContext = realTimeHub;
        }

        public async Task<string> GenerateOtpEFAsync(string phoneNumber)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.phone == phoneNumber);

            if (user == null)
            {
                // ถ้ายังไม่มีผู้ใช้ ให้สร้างใหม่
                user = new User
                {
                    phone = phoneNumber,
                    otp = otp,
                    OtpExpiry = expiry
                };
                await _db.Users.AddAsync(user);
                // _db.Entry(user).State = EntityState.Modified;
                // await _db.SaveChangesAsync();
            }
            else
            {
                // ถ้ามีผู้ใช้อยู่แล้ว อัปเดต OTP และเวลาหมดอายุ
                user.otp = otp;
                user.OtpExpiry = expiry;
                // _db.Entry(user).State = EntityState.Modified;
                // await _db.SaveChangesAsync();
            }

            // try
            // {
            //     var auditLog = new audit_logs
            //     {
            //         entity_name = "User",
            //         action = "CREATE", // e.g., "Created"
            //         changes = JsonDocument.Parse(JsonConvert.SerializeObject(user)), // Store user details in JSON format
            //         timestamp = DateTime.UtcNow,
            //         user_id = user.user_id.ToString() // You can replace this with the actual user performing the action
            //     };
            //     Console.WriteLine($"Audit Log: {JsonConvert.SerializeObject(auditLog)}");
            //     await _auditDb.AuditLogs.AddAsync(auditLog);
            //     await _auditDb.SaveChangesAsync();
            //     await _db.SaveChangesAsync();
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"Error saving OTP: {ex.Message}");
            //     throw;
            // }
            // await TestConnectionAsync();
            await _db.SaveChangesAsync(); // บันทึกข้อมูลลงฐานข้อมูล
            return otp;
        }

        public async Task<string> testsignalr()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "TestUser", "Test Message");
            return "success";
        }

        public async Task<string> GenerateOtpAsync(string phoneNumber)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE phone = @Phone", connection);
                checkCommand.Parameters.AddWithValue("@Phone", phoneNumber);
                var phoneExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                MySqlCommand command;

                if (phoneExists)
                {
                    // Update OTP if phone exists
                    command = new MySqlCommand("UPDATE Users SET otp = @Otp, OtpExpiry = @Expiry WHERE phone = @Phone", connection);
                }
                else
                {
                    // Insert new record if phone doesn't exist
                    command = new MySqlCommand("INSERT INTO Users (phone, otp, OtpExpiry) VALUES (@Phone, @Otp, @Expiry)", connection);
                }

                // Add parameters
                command.Parameters.AddWithValue("@Otp", otp);
                command.Parameters.AddWithValue("@Expiry", expiry);
                command.Parameters.AddWithValue("@Phone", phoneNumber);

                // Execute the command
                await command.ExecuteNonQueryAsync();
            }

            return otp;

            // var otp = new Random().Next(100000, 999999).ToString();
            // var expiry = DateTime.UtcNow.AddMinutes(5);

            // var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            // if (user == null)
            // {
            //     user = new User { PhoneNumber = phoneNumber, Otp = otp, OtpExpiry = expiry };
            //     await _db.Users.AddAsync(user);
            // }
            // else
            // {
            //     user.Otp = otp;
            //     user.OtpExpiry = expiry;
            // }

            // await _db.SaveChangesAsync();
            // return otp;
        }

        public async Task<(string JwtToken, string RefreshToken)> VerifyOtpEFAsync(string phoneNumber, string otp)
        {
            // Fetch the user based on phone number and OTP
            var user = await _db.Users.FirstOrDefaultAsync(u => u.phone == phoneNumber && u.otp == otp);

            // Check if the user doesn't exist or OTP has expired
            if (user == null || user.OtpExpiry < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("OTP ไม่ถูกต้องหรือหมดอายุ");
            }

            // Generate the JWT token
            var jwt = GenerateJwt(user);

            // Generate a new refresh token
            var refreshToken = GenerateRefreshToken();

            // Check if there's already a refresh token for this user
            var existingToken = await _db.RefreshTokens
                                          .FirstOrDefaultAsync(rt => rt.user_id == user.user_id);

            if (existingToken != null)
            {
                // If an existing token is found, update it with the new refresh token
                existingToken.token = refreshToken;
                existingToken.expiry = DateTime.UtcNow.AddDays(7);  // Set a new expiry time
                _db.RefreshTokens.Update(existingToken);

            }
            else
            {
                // If no existing token is found, insert a new refresh token
                var tokenEntry = new RefreshToken
                {
                    user_id = user.user_id,
                    token = refreshToken,
                    expiry = DateTime.UtcNow.AddDays(7)  // Set a new expiry time
                };
                await _db.RefreshTokens.AddAsync(tokenEntry);
            }


            // Mark OTP as used or expired by updating the OTP and expiry
            user.otp = null; // Clear OTP after usage
            user.OtpExpiry = DateTime.UtcNow; // Set expiry to current time to invalidate the OTP
            await _db.SaveChangesAsync(); // Save changes to the user record

            // Save the changes to the RefreshTokens table
            // await _db.SaveChangesAsync();

            // Return the JWT token and the refresh token
            return (jwt, refreshToken);
        }

        public async Task<(string JwtToken, string RefreshToken)> VerifyOtpAsync(string phoneNumber, string otp)
        {
            User? user = null;

            // Step 1: Verify OTP
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // SQL query to find the user by phone number and OTP
                var command = new MySqlCommand("SELECT user_id, phone, OtpExpiry FROM users WHERE phone = @Phone AND otp = @Otp", connection);
                command.Parameters.AddWithValue("@Phone", phoneNumber);
                command.Parameters.AddWithValue("@Otp", otp);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = new User
                        {
                            user_id = reader.GetInt32(0),
                            phone = reader.GetString(1),
                            OtpExpiry = reader.GetDateTime(2)
                        };
                    }
                }
            }

            // Check if the user exists and OTP has expired
            if (user == null || user.OtpExpiry < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("OTP ไม่ถูกต้องหรือหมดอายุ");
            }

            // Step 2: Generate JWT and Refresh Token
            var jwt = GenerateJwt(user);
            var refreshToken = GenerateRefreshToken();

            // Step 3: Insert or update Refresh Token
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Check if there is already a refresh token for this user
                var command = new MySqlCommand("SELECT COUNT(*) FROM RefreshTokens WHERE user_id = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", user.user_id);

                var existingTokenCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                if (existingTokenCount > 0)
                {
                    // Update existing refresh token
                    command = new MySqlCommand("UPDATE RefreshTokens SET token = @Token, expiry = @Expiry WHERE user_id = @UserId", connection);
                    command.Parameters.AddWithValue("@UserId", user.user_id);
                    command.Parameters.AddWithValue("@Token", refreshToken);
                    command.Parameters.AddWithValue("@Expiry", DateTime.UtcNow.AddDays(7));
                    await command.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new refresh token if none exists
                    command = new MySqlCommand("INSERT INTO RefreshTokens (user_id, token, expiry) VALUES (@UserId, @Token, @Expiry)", connection);
                    command.Parameters.AddWithValue("@UserId", user.user_id);
                    command.Parameters.AddWithValue("@Token", refreshToken);
                    command.Parameters.AddWithValue("@Expiry", DateTime.UtcNow.AddDays(7));
                    await command.ExecuteNonQueryAsync();
                }

                var updateOtpCommand = new MySqlCommand("UPDATE users SET otp = NULL, OtpExpiry = @Expiry WHERE user_id = @UserId", connection);
                updateOtpCommand.Parameters.AddWithValue("@Expiry", DateTime.UtcNow); // Set OtpExpiry to current time
                updateOtpCommand.Parameters.AddWithValue("@UserId", user.user_id);

                await updateOtpCommand.ExecuteNonQueryAsync();
            }


            return (jwt, refreshToken);

            // var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Otp == otp);
            // if (user == null || user.OtpExpiry < DateTime.UtcNow)
            //     throw new UnauthorizedAccessException("OTP ไม่ถูกต้องหรือหมดอายุ");

            // var jwt = GenerateJwt(user);
            // var refreshToken = GenerateRefreshToken();

            // var tokenEntry = new RefreshToken
            // {
            //     UserId = user.Id,
            //     Token = refreshToken,
            //     Expiry = DateTime.UtcNow.AddDays(7)
            // };

            // await _db.RefreshTokens.AddAsync(tokenEntry);
            // await _db.SaveChangesAsync();

            // return (jwt, refreshToken);


        }

        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])); // ✅ Use UTF-8 encoding instead of Base64
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
            new Claim(ClaimTypes.MobilePhone, user.phone),
                },
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }

        public async Task TestConnectionAsync()
        {
            try
            {
                // ใช้ DbContext ตรวจสอบการเชื่อมต่อ
                var connection = _db.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                Console.WriteLine("เชื่อมต่อกับ PostgreSQL สำเร็จ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ไม่สามารถเชื่อมต่อกับ PostgreSQL ได้: {ex.Message}");
            }
        }
    }
}
