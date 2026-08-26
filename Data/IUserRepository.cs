using ConfidraApi.Common.Models;

namespace ConfidraApi.Data;

public interface IUserRepository
{
    Task<bool> ExistsByEmailOrPhoneAsync(string email, string phone, CancellationToken cancellationToken);
    Task<User?> FindByEmailOrPhoneAsync(string emailOrPhone, CancellationToken cancellationToken);
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
}
