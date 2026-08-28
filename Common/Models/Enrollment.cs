namespace ConfidraApi.Common.Models;

public sealed class Enrollment
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public DateTime EnrolledUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

public sealed record CreateEnrollmentRequest(int? UserId, string PlanName, string PaymentId);