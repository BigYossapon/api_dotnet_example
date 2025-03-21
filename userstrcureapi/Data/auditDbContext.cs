using Microsoft.EntityFrameworkCore;
using userstrctureapi.Models;

namespace userstrctureapi.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        }
    }
}