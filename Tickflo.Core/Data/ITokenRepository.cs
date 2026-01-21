namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITokenRepository
{
    public Task<Token?> FindByUserIdAsync(int userId);
    public Task<Token?> FindByValueAsync(string value);
    public Task<Token> CreateForUserIdAsync(int userId);
    public Task<Token> CreatePasswordResetForUserIdAsync(int userId, int maxAgeSeconds = 3600);
}
