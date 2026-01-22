namespace Tickflo.Core.Services.Views;

/// <summary>
/// Service for aggregating workspace users/invites view data.
/// </summary>
public interface IWorkspaceUsersViewService
{
    public Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for workspace users page.
/// </summary>
public class WorkspaceUsersViewData
{
    public bool IsWorkspaceAdmin { get; set; }
    public bool CanViewUsers { get; set; }
    public bool CanCreateUsers { get; set; }
    public bool CanEditUsers { get; set; }
    public List<InviteView> PendingInvites { get; set; } = [];
    public List<AcceptedUserView> AcceptedUsers { get; set; } = [];
}

public class InviteView
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class AcceptedUserView
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsAdmin { get; set; }
}


