using ConfidraApi.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ConfidraApi.Data;

public sealed class UserRepository(ConfidraDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phone, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Email == email || user.Phone == phone,
            cancellationToken);
    }

    public Task<User?> FindByEmailOrPhoneAsync(string emailOrPhone, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == emailOrPhone || user.Phone == emailOrPhone,
            cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
