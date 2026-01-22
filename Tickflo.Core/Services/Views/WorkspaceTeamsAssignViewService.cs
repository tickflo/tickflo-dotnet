namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceTeamsAssignViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
    IRolePermissionRepository rolePermissionRepository,
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserRepository userRepository) : IWorkspaceTeamsAssignViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMemberRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserRepository userRepository = userRepository;

    public async Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId)
    {
        var data = new WorkspaceTeamsAssignViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewTeams = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanView);
        data.CanEditTeams = isAdmin || (eff.TryGetValue("teams", out var tp2) && tp2.CanEdit);
        if (!data.CanViewTeams)
        {
            return data;
        }

        data.Team = await this.teamRepository.FindByIdAsync(teamId);
        if (data.Team == null || data.Team.WorkspaceId != workspaceId)
        {
            return data;
        }

        var members = await this.teamMemberRepository.ListMembersAsync(teamId);
        data.Members = [.. members];

        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var id in userIds)
        {
            var user = await this.userRepository.FindByIdAsync(id);
            if (user != null)
            {
                data.WorkspaceUsers.Add(user);
            }
        }

        return data;
    }
}


