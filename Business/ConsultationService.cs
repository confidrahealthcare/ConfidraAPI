using ConfidraApi.Common.Models;
using ConfidraApi.Data;

namespace ConfidraApi.Business;

public sealed class ConsultationService(IConsultationRequestRepository repository)
{
    public async Task<(bool Succeeded, string? Error)> CreateAsync(
        ConsultationRequestInput input,
        CancellationToken cancellationToken)
    {
        var fullName = input.FullName.Trim();
        var phone = input.Phone.Trim();
        var email = input.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
        {
            return (false, "Full name, phone, and email are required.");
        }

        if (!email.Contains('@') || email.Length > 320)
        {
            return (false, "Please provide a valid email address.");
        }

        await repository.AddAsync(new ConsultationRequest
        {
            FullName = fullName,
            Phone = phone,
            Email = email,
            PlanName = string.IsNullOrWhiteSpace(input.PlanName) ? null : input.PlanName.Trim(),
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken);

        return (true, null);
    }
}