using System.ComponentModel.DataAnnotations;
namespace ConfidraApi.Common.Models;
public sealed record RegisterRequest([Required,MaxLength(150)]string FullName,[Required,EmailAddress,MaxLength(320)]string Email,[Required,RegularExpression(@"^\+?[0-9 \-]{10,20}$")]string Phone,[Required,MinLength(12),MaxLength(128)]string Password, bool AdultConfirmed);
public sealed record LoginRequest([Required,MaxLength(320)]string EmailOrPhone,[Required,MaxLength(128)]string Password);
public sealed record PasswordResetRequest([Required,EmailAddress,MaxLength(320)]string Email);
public sealed record VerifyPasswordResetOtpRequest([Required,EmailAddress,MaxLength(320)]string Email,[Required,RegularExpression("^[0-9]{6}$")]string Otp);
public sealed record ResetPasswordRequest([Required,EmailAddress,MaxLength(320)]string Email,[Required,RegularExpression("^[0-9]{6}$")]string Otp,[Required,MinLength(12),MaxLength(128)]string NewPassword);
public sealed record AuthUserResponse(int Id,string FullName,string Email,string Phone);
public sealed record DashboardStatsResponse(int TotalPatients,int ActiveFollowUps,int PartnerClinicians,int CitiesServed);
