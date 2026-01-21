namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IUserRepository
{
    public Task<User?> FindByEmailAsync(string email);
    public Task AddAsync(User user);
    public Task<User?> FindByIdAsync(int userId);
    public Task<List<User>> ListAsync();
    public Task UpdateAsync(User user);
}
