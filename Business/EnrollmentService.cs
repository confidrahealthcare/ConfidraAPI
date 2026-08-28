using ConfidraApi.Common.Models;
using ConfidraApi.Data;

namespace ConfidraApi.Business;

public sealed class EnrollmentService(IEnrollmentRepository repository)
{
    private static readonly Dictionary<string, int> PlanDurations = new()
    {
        ["Confidra Core 90"] = 90,
        ["Confidra Plus 90"] = 90,
        ["Confidra Continuum 365"] = 365
    };

    public async Task<(bool Succeeded, string? Error, Enrollment? Enrollment)> CreateAsync(
        CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!PlanDurations.TryGetValue(request.PlanName, out var duration) || string.IsNullOrWhiteSpace(request.PaymentId))
        {
            return (false, "The enrollment details are invalid.", null);
        }

        var paymentId = request.PaymentId.Trim();
        var existing = await repository.FindByPaymentIdAsync(paymentId, cancellationToken);
        if (existing is not null)
        {
            return (true, null, existing);
        }

        var enrolledUtc = DateTime.UtcNow;
        var enrollment = new Enrollment
        {
            UserId = request.UserId,
            PlanName = request.PlanName,
            PaymentId = paymentId,
            EnrolledUtc = enrolledUtc,
            ExpiresUtc = enrolledUtc.AddDays(duration)
        };
        await repository.AddAsync(enrollment, cancellationToken);
        return (true, null, enrollment);
    }
}