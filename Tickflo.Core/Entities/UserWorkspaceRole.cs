namespace Tickflo.Core.Entities;

public class UserWorkspaceRole
{
    public int UserId { get; set; }
    public int WorkspaceId { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}
