using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TokenRepository(TickfloDbContext db) : ITokenRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<Token?> FindByUserIdAsync(int userId)
    {
        return _db.Tokens.FirstOrDefaultAsync(t => t.UserId == userId);
    }

    public async Task AddAsync(Token token)
    {
        _db.Tokens.Add(token);
        await _db.SaveChangesAsync();
    }
}