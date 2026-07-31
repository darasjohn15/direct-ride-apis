using DirectRide.Api.Models;

namespace DirectRide.Api.Repositories;

public interface IUserRepository
{
    IQueryable<User> Query();
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
