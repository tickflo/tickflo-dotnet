namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceTeamsAssignViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    ITeamRepository teams,
    ITeamMemberRepository members,
    IUserWorkspaceRepository userWorkspaces,
    IUserRepository users) : IWorkspaceTeamsAssignViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly ITeamRepository _teams = teams;
    private readonly ITeamMemberRepository _members = members;
    private readonly IUserWorkspaceRepository _userWorkspaces = userWorkspaces;
    private readonly IUserRepository _users = users;

    public async Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId)
    {
        var data = new WorkspaceTeamsAssignViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewTeams = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanView);
        data.CanEditTeams = isAdmin || (eff.TryGetValue("teams", out var tp2) && tp2.CanEdit);
        if (!data.CanViewTeams)
        {
            return data;
        }

        data.Team = await this._teams.FindByIdAsync(teamId);
        if (data.Team == null || data.Team.WorkspaceId != workspaceId)
        {
            return data;
        }

        var members = await this._members.ListMembersAsync(teamId);
        data.Members = [.. members];

        var memberships = await this._userWorkspaces.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var id in userIds)
        {
            var u = await this._users.FindByIdAsync(id);
            if (u != null)
            {
                data.WorkspaceUsers.Add(u);
            }
        }

        return data;
    }
}


