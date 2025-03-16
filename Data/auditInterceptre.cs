using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using userstrctureapi.Models;
using userstrctureapi.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly AuditDbContext _auditDbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(AuditDbContext auditDbContext, IHttpContextAccessor httpContextAccessor)
    {
        _auditDbContext = auditDbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return result;

        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged) continue;

            var auditLog = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow,
                Changes = GetChanges(entry)
            };

            // ดึง User ID จาก HttpContext (เช่น JWT Token)
            var httpContext = _httpContextAccessor.HttpContext;
            auditLog.UserId = httpContext?.User.Identity?.Name ?? "Unknown";

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Any())
        {
            _auditDbContext.AuditLogs.AddRange(auditLogs);
            await _auditDbContext.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static string GetChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();

        foreach (var property in entry.OriginalValues.Properties)
        {
            var originalValue = entry.OriginalValues[property];
            var currentValue = entry.CurrentValues[property];

            if (!Equals(originalValue, currentValue))
            {
                changes[property.Name] = new { Old = originalValue, New = currentValue };
            }
        }

        return JsonSerializer.Serialize(changes);
    }
}
