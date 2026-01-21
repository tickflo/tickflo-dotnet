namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Workspace;

public class WorkspaceTeamsViewService(
    IWorkspaceAccessService workspaceAccessService,
    ITeamListingService listingService) : IWorkspaceTeamsViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly ITeamListingService _listingService = listingService;

    public async Task<WorkspaceTeamsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceTeamsViewData();

        // Check admin status
        var isAdmin = await this._workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);

        // Get permissions
        var permissions = await this._workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        // Determine view access and action permissions
        data.CanViewTeams = isAdmin;
        if (!data.CanViewTeams && permissions.TryGetValue("teams", out var teamPermissions))
        {
            data.CanViewTeams = teamPermissions.CanView;
            data.CanCreateTeams = teamPermissions.CanCreate;
            data.CanEditTeams = teamPermissions.CanEdit;
        }
        else if (isAdmin)
        {
            data.CanCreateTeams = true;
            data.CanEditTeams = true;
        }

        // Load teams and member counts if user can view
        if (data.CanViewTeams)
        {
            var (teams, memberCounts) = await this._listingService.GetListAsync(workspaceId);
            data.Teams = [.. teams];
            data.MemberCounts = memberCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return data;
    }
}



