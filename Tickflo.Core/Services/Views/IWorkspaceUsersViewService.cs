using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

/// <summary>
/// Service for aggregating workspace users/invites view data.
/// </summary>
public interface IWorkspaceUsersViewService
{
    Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for workspace users page.
/// </summary>
public class WorkspaceUsersViewData
{
    public bool IsWorkspaceAdmin { get; set; }
    public bool CanCreateUsers { get; set; }
    public bool CanEditUsers { get; set; }
    public List<InviteView> PendingInvites { get; set; } = new();
}

public class InviteView
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}


