public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // ดึง User ID จาก Token
        var userId = context.User.Identity?.Name ?? "Unknown";

        // เก็บ API Path และ Method ใน HttpContext
        context.Items["AuditUserId"] = userId;
        context.Items["AuditEndpoint"] = context.Request.Path;
        context.Items["AuditHttpMethod"] = context.Request.Method;

        await _next(context);
    }
}