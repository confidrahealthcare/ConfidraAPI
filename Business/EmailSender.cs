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
        Host = Read(configuration, "Smtp:Host", "EmailSettings:SmtpHost"),
        Port = int.TryParse(Read(configuration, "Smtp:Port", "EmailSettings:SmtpPort"), out var port) ? port : 587,
        Username = Read(configuration, "Smtp:Username", "EmailSettings:Username"),
        Password = Read(configuration, "Smtp:Password", "EmailSettings:Password"),
        FromEmail = Read(configuration, "Smtp:FromEmail", "EmailSettings:FromEmail"),
        FromName = Read(configuration, "Smtp:FromName", "EmailSettings:FromName", "Confidra"),
        EnableSsl = !bool.TryParse(Read(configuration, "Smtp:EnableSsl", "EmailSettings:UseStartTls"), out var enableSsl) || enableSsl
    };

    private static string Read(IConfiguration configuration, string primaryKey, string fallbackKey, string defaultValue = "") =>
        configuration[primaryKey] ?? configuration[fallbackKey] ?? defaultValue;

    public async Task SendPasswordResetOtpAsync(string recipient, string otp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP is not configured. Set the Smtp or EmailSettings section in appsettings.Development.json.");
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
