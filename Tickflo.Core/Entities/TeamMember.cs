namespace Tickflo.Core.Entities;

public class TeamMember
{
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
