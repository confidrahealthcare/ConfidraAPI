using ConfidraApi.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ConfidraApi.Data;

public sealed class ConfidraDbContext(DbContextOptions<ConfidraDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ConsultationRequest> ConsultationRequests => Set<ConsultationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.Phone).HasMaxLength(30).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.CreatedUtc).IsRequired();
            entity.Property(user => user.PasswordResetOtpHash).HasMaxLength(64);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.Phone).IsUnique();
        });

        modelBuilder.Entity<ConsultationRequest>(entity =>
        {
            entity.ToTable("ConsultationRequests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.FullName).HasMaxLength(150).IsRequired();
            entity.Property(request => request.Phone).HasMaxLength(30).IsRequired();
            entity.Property(request => request.Email).HasMaxLength(320).IsRequired();
            entity.Property(request => request.PlanName).HasMaxLength(100);
            entity.Property(request => request.CreatedUtc).IsRequired();
            entity.HasIndex(request => request.CreatedUtc);
        });
    }
}
