namespace ConfidraApi.Common.Models;

public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string? PasswordResetOtpHash { get; set; }
    public DateTime? PasswordResetOtpExpiresUtc { get; set; }
}
