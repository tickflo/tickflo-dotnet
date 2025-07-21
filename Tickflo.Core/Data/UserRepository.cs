using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class UserRepository(TickfloDbContext db) : IUserRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<User?> FindByEmailAsync(string email)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }
}