namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Token
{
    public int UserId { get; set; }

    [Column("token")]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public int MaxAge { get; set; }
}
