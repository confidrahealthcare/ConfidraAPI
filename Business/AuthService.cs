using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace ConfidraApi.Business;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IEmailSender emailSender)
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

    public async Task<(bool Succeeded, string? Error)> RequestPasswordResetAsync(
        PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email is required.");
        }

        var user = await userRepository.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return (false, "No account was found with that email.");
        }

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.PasswordResetOtpHash = HashOtp(otp);
        user.PasswordResetOtpExpiresUtc = DateTime.UtcNow.AddMinutes(10);
        await userRepository.SaveAsync(cancellationToken);
        try
        {
            await emailSender.SendPasswordResetOtpAsync(user.Email, otp, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            user.PasswordResetOtpHash = null;
            user.PasswordResetOtpExpiresUtc = null;
            await userRepository.SaveAsync(cancellationToken);
            return (false, "Password reset email is not configured. Add the SMTP settings and try again.");
        }
        catch (SmtpException)
        {
            user.PasswordResetOtpHash = null;
            user.PasswordResetOtpExpiresUtc = null;
            await userRepository.SaveAsync(cancellationToken);
            return (false, "The password reset email could not be sent. Check the SMTP settings and try again.");
        }

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> VerifyPasswordResetOtpAsync(
        VerifyPasswordResetOtpRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindUserForResetAsync(request.Email, cancellationToken);
        return ValidateOtp(user, request.Otp);
    }

    public async Task<(bool Succeeded, string? Error)> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return (false, "Password must be at least 8 characters long.");
        }

        var user = await FindUserForResetAsync(request.Email, cancellationToken);
        var otpResult = ValidateOtp(user, request.Otp);
        if (!otpResult.Succeeded || user is null)
        {
            return otpResult;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordResetOtpHash = null;
        user.PasswordResetOtpExpiresUtc = null;
        await userRepository.SaveAsync(cancellationToken);
        return (true, null);
    }

    private Task<User?> FindUserForResetAsync(string email, CancellationToken cancellationToken)
    {
        return userRepository.FindByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
    }

    private static (bool Succeeded, string? Error) ValidateOtp(User? user, string otp)
    {
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordResetOtpHash) ||
            user.PasswordResetOtpExpiresUtc is null || user.PasswordResetOtpExpiresUtc <= DateTime.UtcNow)
        {
            return (false, "That code is invalid or has expired.");
        }

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(user.PasswordResetOtpHash),
            Convert.FromHexString(HashOtp(otp.Trim())))
            ? (true, null)
            : (false, "That code is invalid or has expired.");
    }

    private static string HashOtp(string otp) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));

    private static AuthUserResponse ToResponse(User user) =>
        new(user.Id, user.FullName, user.Email, user.Phone);
}
