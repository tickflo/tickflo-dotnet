namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceTeamsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    ITeamRepository teamRepo,
    IUserWorkspaceRepository userWorkspaces,
    IUserRepository userRepo,
    ITeamMemberRepository teamMembers) : IWorkspaceTeamsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly ITeamRepository _teamRepo = teamRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces = userWorkspaces;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ITeamMemberRepository _teamMembers = teamMembers;

    public async Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0)
    {
        var data = new WorkspaceTeamsEditViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewTeams = data.CanEditTeams = data.CanCreateTeams = true;
        }
        else if (eff.TryGetValue("teams", out var tp))
        {
            data.CanViewTeams = tp.CanView;
            data.CanEditTeams = tp.CanEdit;
            data.CanCreateTeams = tp.CanCreate;
        }

        // Load workspace users
        var memberships = await this._userWorkspaces.FindForWorkspaceAsync(workspaceId);
        if (memberships != null)
        {
            foreach (var m in memberships.Select(m => m.UserId).Distinct())
            {
                var u = await this._userRepo.FindByIdAsync(m);
                if (u != null)
                {
                    data.WorkspaceUsers.Add(u);
                }
            }
        }

        if (teamId > 0)
        {
            data.ExistingTeam = await this._teamRepo.FindByIdAsync(teamId);
            if (data.ExistingTeam != null)
            {
                var members = await this._teamMembers.ListMembersAsync(teamId);
                data.ExistingMemberIds = [.. members.Select(m => m.Id)];
            }
        }

        return data;
    }
}


