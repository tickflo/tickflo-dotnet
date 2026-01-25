namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using Tickflo.Core.Utils;

public class Token
{
    public int UserId { get; set; }

    [Column("token")]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int MaxAge { get; set; }

    private Token()
    {
    }

    public Token(int userId, int maxAgeInSeconds)
    {
        this.UserId = userId;
        this.Value = TokenGenerator.GenerateToken(16);
        this.MaxAge = maxAgeInSeconds;
    }
}
