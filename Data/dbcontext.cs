using Microsoft.EntityFrameworkCore;
using userstrctureapi.Models;
namespace userstrctureapi.Data
{


    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

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
    }
}