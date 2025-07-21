namespace Tickflo.Core.Entities;

public class Token
{
    public int UserId { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MaxAge { get; set; }
}