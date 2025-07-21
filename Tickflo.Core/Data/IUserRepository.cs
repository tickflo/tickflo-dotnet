using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task AddAsync(User user);
}