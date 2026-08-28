using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfidraApi.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ConfidraDb")
            ?? throw new InvalidOperationException("ConnectionStrings:ConfidraDb is not configured.");

        services.AddDbContext<ConfidraDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConsultationRequestRepository, ConsultationRequestRepository>();
        services.AddScoped<IStatsRepository, StatsRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        return services;
    }
}
