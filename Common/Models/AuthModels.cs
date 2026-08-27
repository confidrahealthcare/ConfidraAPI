namespace ConfidraApi.Common.Models;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Phone,
    string Password);

public sealed record LoginRequest(
    string EmailOrPhone,
    string Password);

public sealed record PasswordResetRequest(string Email);

public sealed record VerifyPasswordResetOtpRequest(string Email, string Otp);

public sealed record ResetPasswordRequest(string Email, string Otp, string NewPassword);

public sealed record AuthUserResponse(
    int Id,
    string FullName,
    string Email,
    string Phone);

public sealed record DashboardStatsResponse(
    int PatientsOnboarded,
    int ProgramFollowUp,
    int RegisteredClinicians,
    int CitiesServed);
