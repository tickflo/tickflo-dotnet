namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserRepository(TickfloDbContext db) : IUserRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<User?> FindByEmailAsync(string email) => this._db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        this._db.Users.Add(user);
        await this._db.SaveChangesAsync();
    }

    public Task<User?> FindByIdAsync(int userId) => this._db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public Task<List<User>> ListAsync() => this._db.Users
            .OrderBy(u => u.Name)
            .ToListAsync();

    public async Task UpdateAsync(User user)
    {
        this._db.Users.Update(user);
        await this._db.SaveChangesAsync();
    }
}
