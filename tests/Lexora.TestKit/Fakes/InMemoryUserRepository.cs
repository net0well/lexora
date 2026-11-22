using Lexora.Domain.Users;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyList<User> Users => _users;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(user => user.Email == email));

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Any(user => user.Email == email));

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_users.Count(user => user.Role == UserRole.Admin && user.IsActive));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);

        return Task.CompletedTask;
    }
}
