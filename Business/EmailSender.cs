using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace ConfidraApi.Business;

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Confidra";
    public bool EnableSsl { get; set; } = true;
}

public interface IEmailSender
{
    Task SendPasswordResetOtpAsync(string recipient, string otp, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private readonly SmtpOptions settings = new()
    {
        Host = configuration["Smtp:Host"] ?? string.Empty,
        Port = int.TryParse(configuration["Smtp:Port"], out var port) ? port : 587,
        Username = configuration["Smtp:Username"] ?? string.Empty,
        Password = configuration["Smtp:Password"] ?? string.Empty,
        FromEmail = configuration["Smtp:FromEmail"] ?? string.Empty,
        FromName = configuration["Smtp:FromName"] ?? "Confidra",
        EnableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var enableSsl) || enableSsl
    };

    public async Task SendPasswordResetOtpAsync(string recipient, string otp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP is not configured. Set the Smtp section in appsettings.Development.json.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = "Your Confidra password reset code",
            Body = $"Your Confidra password reset code is {otp}. It expires in 10 minutes.",
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
