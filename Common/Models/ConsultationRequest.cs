namespace ConfidraApi.Common.Models;

public sealed class ConsultationRequest
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public sealed record ConsultationRequestInput(string FullName, string Phone, string Email);