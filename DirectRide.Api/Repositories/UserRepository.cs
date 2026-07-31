using DirectRide.Api.Data;
using DirectRide.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<User> Query()
    {
        return _db.Users.AsQueryable();
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users.FindAsync([id], cancellationToken);
    }

    public void Add(User user)
    {
        _db.Users.Add(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
