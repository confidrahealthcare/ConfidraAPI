using ConfidraApi.Common.Models;
using ConfidraApi.Data;

namespace ConfidraApi.Business;

public sealed class AppointmentService(IAppointmentRepository repository, IEnrollmentRepository enrollmentRepository)
{
    private static readonly HashSet<string> AllowedPlans =
    ["Confidra Core 90", "Confidra Plus 90", "Confidra Continuum 365"];

    public async Task<(bool Succeeded, string? Error)> BookAsync(
        BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var enrollment = request.EnrollmentId is null ? null : await enrollmentRepository.FindAsync(request.EnrollmentId.Value, cancellationToken);
        if (enrollment is null || enrollment.PlanName != request.PlanName || enrollment.PaymentId != request.PaymentId ||
            !AllowedPlans.Contains(request.PlanName) || string.IsNullOrWhiteSpace(request.PaymentId) ||
            string.IsNullOrWhiteSpace(request.AppointmentTime) || string.IsNullOrWhiteSpace(request.DoctorName))
        {
            return (false, "The payment or programme details are invalid.");
        }

        if (request.AppointmentDate.Date < DateTime.UtcNow.Date || request.AppointmentDate.Date >= enrollment.ExpiresUtc.Date)
        {
            return (false, $"Please select a date from today through {enrollment.ExpiresUtc.AddDays(-1):dd MMM yyyy}.");
        }

        await repository.AddAsync(new Appointment
        {
            UserId = request.UserId,
            PlanName = request.PlanName,
            PaymentId = request.PaymentId.Trim(),
            AppointmentDate = request.AppointmentDate.Date,
            AppointmentTime = request.AppointmentTime.Trim(),
            DoctorName = request.DoctorName.Trim(),
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken);

        return (true, "Your doctor consultation is booked successfully.");
    }

    public Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId, CancellationToken cancellationToken) =>
        repository.GetForUserAsync(userId, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CancelAsync(
        int appointmentId,
        int userId,
        CancellationToken cancellationToken)
    {
        var appointment = await repository.FindForUserAsync(appointmentId, userId, cancellationToken);
        if (appointment is null)
        {
            return (false, "Appointment not found.");
        }

        if (appointment.AppointmentDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Past appointments cannot be cancelled.");
        }

        appointment.Status = "Cancelled";
        await repository.SaveAsync(cancellationToken);
        return (true, "Appointment cancelled successfully.");
    }
}