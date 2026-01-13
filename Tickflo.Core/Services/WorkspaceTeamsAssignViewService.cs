using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceTeamsAssignViewService : IWorkspaceTeamsAssignViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ITeamRepository _teams;
    private readonly ITeamMemberRepository _members;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _users;

    public WorkspaceTeamsAssignViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        ITeamRepository teams,
        ITeamMemberRepository members,
        IUserWorkspaceRepository userWorkspaces,
        IUserRepository users)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _teams = teams;
        _members = members;
        _userWorkspaces = userWorkspaces;
        _users = users;
    }

    public async Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId)
    {
        var data = new WorkspaceTeamsAssignViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewTeams = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanView);
        data.CanEditTeams = isAdmin || (eff.TryGetValue("teams", out var tp2) && tp2.CanEdit);
        if (!data.CanViewTeams) return data;

        data.Team = await _teams.FindByIdAsync(teamId);
        if (data.Team == null || data.Team.WorkspaceId != workspaceId) return data;

        var members = await _members.ListMembersAsync(teamId);
        data.Members = members.ToList();

        var memberships = await _userWorkspaces.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var id in userIds)
        {
            var u = await _users.FindByIdAsync(id);
            if (u != null) data.WorkspaceUsers.Add(u);
        }

        return data;
    }
}
