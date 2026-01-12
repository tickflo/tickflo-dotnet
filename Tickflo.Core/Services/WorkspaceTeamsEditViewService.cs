using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceTeamsEditViewService : IWorkspaceTeamsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ITeamRepository _teamRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _userRepo;
    private readonly ITeamMemberRepository _teamMembers;

    public WorkspaceTeamsEditViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        ITeamRepository teamRepo,
        IUserWorkspaceRepository userWorkspaces,
        IUserRepository userRepo,
        ITeamMemberRepository teamMembers)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _teamRepo = teamRepo;
        _userWorkspaces = userWorkspaces;
        _userRepo = userRepo;
        _teamMembers = teamMembers;
    }

    public async Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0)
    {
        var data = new WorkspaceTeamsEditViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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
        var memberships = await _userWorkspaces.FindForWorkspaceAsync(workspaceId);
        if (memberships != null)
        {
            foreach (var m in memberships.Select(m => m.UserId).Distinct())
            {
                var u = await _userRepo.FindByIdAsync(m);
                if (u != null) data.WorkspaceUsers.Add(u);
            }
        }

        if (teamId > 0)
        {
            data.ExistingTeam = await _teamRepo.FindByIdAsync(teamId);
            if (data.ExistingTeam != null)
            {
                var members = await _teamMembers.ListMembersAsync(teamId);
                data.ExistingMemberIds = members.Select(m => m.Id).ToList();
            }
        }

        return data;
    }
}
