namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Entities;
using Tickflo.Core.Utils;

public class TokenRepository(TickfloDbContext db, TickfloConfig config) : ITokenRepository
{
    private readonly TickfloDbContext _db = db;
    private readonly TickfloConfig _config = config;

    public Task<Token?> FindByUserIdAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return this._db.Tokens
            .Where(t => t.UserId == userId && now < t.CreatedAt.AddSeconds(t.MaxAge))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<Token?> FindByValueAsync(string value)
    {
        var now = DateTime.UtcNow;
        return this._db.Tokens
            .Where(t => t.Value == value && now < t.CreatedAt.AddSeconds(t.MaxAge))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Token> CreateForUserIdAsync(int userId)
    {
        var token = new Token
        {
            UserId = userId,
            Value = TokenGenerator.GenerateToken(),
            MaxAge = this._config.SessionTimeoutMinutes * 60,
            CreatedAt = DateTime.UtcNow
        };

        this._db.Tokens.Add(token);
        await this._db.SaveChangesAsync();

        return token;
    }

    public async Task<Token> CreatePasswordResetForUserIdAsync(int userId, int maxAgeSeconds = 3600)
    {
        var token = new Token
        {
            UserId = userId,
            Value = TokenGenerator.GenerateToken(),
            MaxAge = maxAgeSeconds,
            CreatedAt = DateTime.UtcNow
        };

        this._db.Tokens.Add(token);
        await this._db.SaveChangesAsync();

        return token;
    }
}
