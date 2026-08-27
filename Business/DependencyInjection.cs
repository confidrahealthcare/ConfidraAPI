using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfidraApi.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDataServices(configuration);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<AuthService>();
        services.AddScoped<ConsultationService>();
        services.AddScoped<StatsService>();

        return services;
    }
}
