namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceTeamsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    ITeamRepository teamRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserRepository userRepository,
    ITeamMemberRepository teamMembers) : IWorkspaceTeamsEditViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMembers;

    public async Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0)
    {
        var data = new WorkspaceTeamsEditViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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
        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        if (memberships != null)
        {
            foreach (var m in memberships.Select(m => m.UserId).Distinct())
            {
                var u = await this.userRepository.FindByIdAsync(m);
                if (u != null)
                {
                    data.WorkspaceUsers.Add(u);
                }
            }
        }

        if (teamId > 0)
        {
            data.ExistingTeam = await this.teamRepository.FindByIdAsync(teamId);
            if (data.ExistingTeam != null)
            {
                var members = await this.teamMemberRepository.ListMembersAsync(teamId);
                data.ExistingMemberIds = [.. members.Select(m => m.Id)];
            }
        }

        return data;
    }
}


