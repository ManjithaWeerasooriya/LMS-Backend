using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace LMS_Backend.Data;

public class ApplicationDBContext : IdentityDbContext<User>
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options){}

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unique constraint on UserId + DeviceId to ensure one refresh token per device per user
        builder.Entity<RefreshToken>()
            .HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique();
    }
}