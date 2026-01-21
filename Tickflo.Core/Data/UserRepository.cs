namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserRepository(TickfloDbContext dbContext) : IUserRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<User?> FindByEmailAsync(string email) => this.dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        this.dbContext.Users.Add(user);
        await this.dbContext.SaveChangesAsync();
    }

    public Task<User?> FindByIdAsync(int userId) => this.dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public Task<List<User>> ListAsync() => this.dbContext.Users
            .OrderBy(u => u.Name)
            .ToListAsync();

    public async Task UpdateAsync(User user)
    {
        this.dbContext.Users.Update(user);
        await this.dbContext.SaveChangesAsync();
    }
}
