using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using userstrctureapi.Models;
namespace userstrctureapi.Data
{


    public class AppDbContext : DbContext
    {
        // public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        // public DbSet<auditLog> AuditLogs { get; set; } = default!;
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        private readonly AuditInterceptor _auditInterceptor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditDbContext _auditDbContext;

        public AppDbContext(IHttpContextAccessor httpContextAccessor, AuditDbContext auditDbContext, DbContextOptions<AppDbContext> options, AuditInterceptor auditInterceptor)
            : base(options)
        {
            _auditInterceptor = auditInterceptor;
            _auditDbContext = auditDbContext;
            _httpContextAccessor = httpContextAccessor;
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.user_id)
                .OnDelete(DeleteBehavior.Cascade);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditInterceptor);
        }

        public override int SaveChanges()
        {
            TrackAuditLogs();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TrackAuditLogs();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void TrackAuditLogs()
        {
            var auditLogs = new List<AuditLog>();
            var auditEndpoint = _httpContextAccessor.HttpContext?.Items["AuditEndpoint"]?.ToString();
            Console.WriteLine(auditEndpoint);
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                var tableName = entry.Metadata.GetTableName();
                // if(tableName == ){

                // }
                var auditLog = new AuditLog
                {
                    EntityName = entry.Metadata.GetTableName(),
                    Action = entry.State.ToString(),
                    Changes = JsonDocument.Parse(GetChanges(entry)),
                    UserId = "admin" // ต้องเปลี่ยนให้เป็น user ที่ล็อกอินจริงๆ
                };
                auditLogs.Add(auditLog);
            }

            if (auditLogs.Any())
            {
                _auditDbContext.AuditLogs.AddRange(auditLogs);
                _auditDbContext.SaveChanges(); // บันทึกลง PostgreSQL
            }
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
}