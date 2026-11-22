using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(LexoraDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
        context.Users.CountAsync(user => user.Role == UserRole.Admin && user.Status == UserStatus.Active, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await context.Users.AddAsync(user, cancellationToken);
}
