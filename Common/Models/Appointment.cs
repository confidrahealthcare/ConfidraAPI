namespace ConfidraApi.Common.Models;

public sealed class Appointment
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string AppointmentTime { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Status { get; set; } = "Confirmed";
    public DateTime CreatedUtc { get; set; }
}

public sealed record BookAppointmentRequest(int? UserId, string PlanName, string PaymentId, DateTime AppointmentDate, string AppointmentTime, string DoctorName, int? EnrollmentId);
