namespace Tickflo.Core.Entities;

public class UserWorkspace
{
    public int UserId { get; set; }
    public int WorkspaceId { get; set; }
    public bool Accepted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public Workspace Workspace { get; set; } = new Workspace();
}
