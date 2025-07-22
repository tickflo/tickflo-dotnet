using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITokenRepository
{
    Task<Token?> FindByUserIdAsync(int userId);
    Task<Token?> FindByValueAsync(string value);
    Task<Token> CreateForUserIdAsync(int userId);
}