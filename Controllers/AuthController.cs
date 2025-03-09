using Microsoft.AspNetCore.Mvc;
using userstrctureapi.Services;
public class VerifyOtpRequest
{
    public string otp { get; set; }
    public string phone { get; set; }
}
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
        //dsads
    }

    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] string phoneNumber)
    {
        try
        {
            var otp = await _authService.GenerateOtpAsync(phoneNumber);
            return Ok(new { message = "OTP Sent", otp }); // **(ไม่ควรส่ง OTP กลับมาใน API จริง)**
        }
        catch (System.Exception)
        {
            return Unauthorized(new { message = "OTP ไม่ถูกต้องหรือหมดอายุ" });

        }




    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            var result = await _authService.VerifyOtpAsync(request.phone.ToString(), request.otp.ToString());
            return Ok(new { jwt = result.JwtToken, refreshToken = result.RefreshToken });
        }
        catch (System.Exception)
        {
            return Unauthorized(new { msg = "otp ไม่ถูกต้องหรือหมดอายุ กรุณาลองใหม่" });

        }

    }


    [HttpPost("request-otp-ef")]
    public async Task<IActionResult> RequestOtpEF([FromBody] string phoneNumber)
    {
        // try
        // {
        var otp = await _authService.GenerateOtpEFAsync(phoneNumber);
        return Ok(new { message = "OTP Sent", otp }); // **(ไม่ควรส่ง OTP กลับมาใน API จริง)**
                                                      // }
                                                      // catch (System.Exception)
                                                      // {
                                                      // return Unauthorized(new { message = "OTP ไม่ถูกต้องหรือหมดอายุ" });

        // }




    }

    [HttpPost("verify-otp-ef")]
    public async Task<IActionResult> VerifyOtpEF([FromBody] VerifyOtpRequest request)
    {
        try
        {
            var result = await _authService.VerifyOtpEFAsync(request.phone.ToString(), request.otp.ToString());
            return Ok(new { jwt = result.JwtToken, refreshToken = result.RefreshToken });
        }
        catch (System.Exception)
        {
            return Unauthorized(new { msg = "otp ไม่ถูกต้องหรือหมดอายุ กรุณาลองใหม่" });

        }

    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        // **(ต้องมีการตรวจสอบว่า refreshToken นี้ถูกต้องและยังไม่หมดอายุ)**
        return Ok(new { jwt = "new-jwt-token" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        // **(ลบ refreshToken ออกจากฐานข้อมูล)**
        return Ok(new { message = "Logged out" });
    }
}
