using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using userstrctureapi.Models;
using userstrctureapi.Data;

// public class AuditInterceptor : SaveChangesInterceptor
// {
//     private readonly AuditDbContext _auditDbContext;

//     private readonly IHttpContextAccessor _httpContextAccessor;
//     public AuditInterceptor(IHttpContextAccessor httpContextAccessor, AuditDbContext auditDbContext)
//     {
//         _auditDbContext = auditDbContext;
//         _httpContextAccessor = httpContextAccessor;
//     }

//     public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
//     {
//         var auditLogs = new List<AuditLog>();
//         var auditEndpoint = _httpContextAccessor.HttpContext?.Items["AuditEndpoint"]?.ToString();
//         Console.WriteLine(auditEndpoint);

//         foreach (var entry in eventData.Context.ChangeTracker.Entries())
//         {
//             if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
//             {
//                 var auditLog = new AuditLog
//                 {
//                     EntityName = entry.Entity.GetType().Name,
//                     Action = entry.State.ToString(),
//                     Changes = JsonDocument.Parse(GetChanges(entry)),
//                     Timestamp = DateTime.UtcNow,
//                     UserId = "System" // ต้องใช้ Auth Middleware เพื่อดึง UserId จริง
//                 };

//                 auditLogs.Add(auditLog);
//             }
//         }

//         if (auditLogs.Count > 0)
//         {
//             // _auditDbContext.AuditLogs.AddRange(auditLogs);
//             // _auditDbContext.SaveChanges();
//             eventData.Context.Set<AuditLog>().AddRange(auditLogs);
//             eventData.Context.SaveChanges();
//         }

//         return base.SavedChangesAsync(eventData, result, cancellationToken);
//     }

//     private string GetChanges(EntityEntry entry)
//     {
//         var changes = new Dictionary<string, object>();

//         if (entry.State == EntityState.Modified)
//         {
//             var oldValues = new Dictionary<string, object>();
//             var newValues = new Dictionary<string, object>();

//             foreach (var prop in entry.Properties)
//             {
//                 if (prop.IsModified)
//                 {
//                     oldValues[prop.Metadata.Name] = prop.OriginalValue;
//                     newValues[prop.Metadata.Name] = prop.CurrentValue;
//                 }
//             }

//             changes["OldValue"] = oldValues;
//             changes["NewValue"] = newValues;
//         }
//         else if (entry.State == EntityState.Added)
//         {
//             var newValues = new Dictionary<string, object>();

//             foreach (var prop in entry.Properties)
//             {
//                 newValues[prop.Metadata.Name] = prop.CurrentValue;
//             }

//             changes["NewValue"] = newValues;
//         }
//         else if (entry.State == EntityState.Deleted)
//         {
//             var oldValues = new Dictionary<string, object>();

//             foreach (var prop in entry.Properties)
//             {
//                 oldValues[prop.Metadata.Name] = prop.OriginalValue;
//             }

//             changes["OldValue"] = oldValues;
//         }

//         return JsonSerializer.Serialize(changes, new JsonSerializerOptions { WriteIndented = true });
//     }

// }


public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuditDbContext _auditDbContext;



    public AuditInterceptor(IHttpContextAccessor httpContextAccessor, AuditDbContext auditDbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _auditDbContext = auditDbContext;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Interceptor triggered for DbContext: {eventData.Context.GetType().Name}");
        // var context = eventData.Context;
        // if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);
        eventData.Context.ChangeTracker.DetectChanges();
        var auditLogs = new List<AuditLog>();
        // var context = eventData.Context;
        // if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {

            Console.WriteLine($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),
                    Changes = JsonDocument.Parse(GetChanges(entry)),
                    Timestamp = DateTime.UtcNow,
                    UserId = "System" // You should replace this with actual User ID from Auth Middleware
                };

                auditLogs.Add(auditLog);
            }
        }

        if (auditLogs.Count > 0)
        {
            await _auditDbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
            await _auditDbContext.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private string GetChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();

        if (entry.State == EntityState.Modified)
        {
            var oldValues = new Dictionary<string, object>();
            var newValues = new Dictionary<string, object>();

            foreach (var prop in entry.Properties)
            {
                if (prop.IsModified)
                {
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            changes["OldValue"] = oldValues;
            changes["NewValue"] = newValues;
        }
        else if (entry.State == EntityState.Added)
        {
            var newValues = new Dictionary<string, object>();

            foreach (var prop in entry.Properties)
            {
                newValues[prop.Metadata.Name] = prop.CurrentValue;
            }

            changes["NewValue"] = newValues;
        }
        else if (entry.State == EntityState.Deleted)
        {
            var oldValues = new Dictionary<string, object>();

            foreach (var prop in entry.Properties)
            {
                oldValues[prop.Metadata.Name] = prop.OriginalValue;
            }

            changes["OldValue"] = oldValues;
        }

        return JsonSerializer.Serialize(changes, new JsonSerializerOptions { WriteIndented = true });
    }
}