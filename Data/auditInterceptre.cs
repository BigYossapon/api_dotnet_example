using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using userstrctureapi.Models;
using userstrctureapi.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly AuditDbContext _auditDbContext;

    public AuditInterceptor(AuditDbContext auditDbContext)
    {
        _auditDbContext = auditDbContext;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var auditLogs = new List<AuditLog>();

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),
                    Changes = JsonDocument.Parse(GetChanges(entry)),
                    Timestamp = DateTime.UtcNow,
                    UserId = "System" // ต้องใช้ Auth Middleware เพื่อดึง UserId จริง
                };

                auditLogs.Add(auditLog);
            }
        }

        if (auditLogs.Count > 0)
        {
            _auditDbContext.AuditLogs.AddRange(auditLogs);
            _auditDbContext.SaveChanges();
        }

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private string GetChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();

        foreach (var prop in entry.Properties)
        {
            if (entry.State == EntityState.Modified && prop.IsModified)
            {
                changes[prop.Metadata.Name] = new { OldValue = prop.OriginalValue, NewValue = prop.CurrentValue };
            }
            else if (entry.State == EntityState.Added)
            {
                changes[prop.Metadata.Name] = new { NewValue = prop.CurrentValue };
            }
        }

        return JsonSerializer.Serialize(changes);
    }
}
