using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Identity;

namespace ConfidraApi.Business;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher)
{
    public async Task<(bool Succeeded, string? Error, AuthUserResponse? User)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "Full name, email, phone, and password are required.", null);
        }

        if (request.Password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long.", null);
        }

        if (await userRepository.ExistsByEmailOrPhoneAsync(email, phone, cancellationToken))
        {
            return (false, "An account already exists with that email or phone.", null);
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            CreatedUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.AddAsync(user, cancellationToken);
        return (true, null, ToResponse(user));
    }

    public async Task<(bool Succeeded, string? Error, AuthUserResponse? User)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var identifier = request.EmailOrPhone.Trim().ToLowerInvariant();
        var user = await userRepository.FindByEmailOrPhoneAsync(identifier, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "Invalid email/phone or password.", null);
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return (false, "Invalid email/phone or password.", null);
        }

        return (true, null, ToResponse(user));
    }

    private static AuthUserResponse ToResponse(User user) =>
        new(user.Id, user.FullName, user.Email, user.Phone);
}
