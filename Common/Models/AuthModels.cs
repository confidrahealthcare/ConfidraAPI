namespace ConfidraApi.Common.Models;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Phone,
    string Password);

public sealed record LoginRequest(
    string EmailOrPhone,
    string Password);

public sealed record AuthUserResponse(
    int Id,
    string FullName,
    string Email,
    string Phone);
