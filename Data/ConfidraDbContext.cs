using ConfidraApi.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ConfidraApi.Data;

public sealed class ConfidraDbContext(DbContextOptions<ConfidraDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ConsultationRequest> ConsultationRequests => Set<ConsultationRequest>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

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

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(appointment => appointment.Id);
            entity.Property(appointment => appointment.PlanName).HasMaxLength(100).IsRequired();
            entity.Property(appointment => appointment.PaymentId).HasMaxLength(100).IsRequired();
            entity.Property(appointment => appointment.AppointmentDate).IsRequired();
            entity.Property(appointment => appointment.AppointmentTime).HasMaxLength(20).IsRequired();
            entity.Property(appointment => appointment.DoctorName).HasMaxLength(150).IsRequired();
            entity.Property(appointment => appointment.Status).HasMaxLength(20).IsRequired();
            entity.Property(appointment => appointment.CreatedUtc).IsRequired();
            entity.HasIndex(appointment => appointment.AppointmentDate);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(enrollment => enrollment.Id);
            entity.Property(enrollment => enrollment.PlanName).HasMaxLength(100).IsRequired();
            entity.Property(enrollment => enrollment.PaymentId).HasMaxLength(100).IsRequired();
            entity.Property(enrollment => enrollment.EnrolledUtc).IsRequired();
            entity.Property(enrollment => enrollment.ExpiresUtc).IsRequired();
            entity.HasIndex(enrollment => enrollment.PaymentId).IsUnique();
        });
    }
}
