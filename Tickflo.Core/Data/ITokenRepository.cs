using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITokenRepository
{
    Task<Token?> FindByUserIdAsync(int userId);
    Task<Token> CreateForUserIdAsync(int userId);
}