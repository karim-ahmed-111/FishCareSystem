using FishCareSystem.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace FishCareSystem.API.Data
{


    public class FishCareDbContext : IdentityDbContext<ApplicationUser>
    {
        public FishCareDbContext(DbContextOptions<FishCareDbContext> options) : base(options) { }

        public DbSet<Farm> Farms { get; set; }
        public DbSet<Tank> Tanks { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tank>()
                .HasOne(t => t.Farm)
                .WithMany(f => f.Tanks)
                .HasForeignKey(t => t.FarmId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SensorReading>()
                .HasOne(sr => sr.Tank)
                .WithMany(t => t.SensorReadings)
                .HasForeignKey(sr => sr.TankId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Device>()
                .HasOne(d => d.Tank)
                .WithMany(t => t.Devices)
                .HasForeignKey(d => d.TankId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Alert>()
                .HasOne(a => a.Tank)
                .WithMany(t => t.Alerts)
                .HasForeignKey(a => a.TankId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
