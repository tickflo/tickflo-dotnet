using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Teams;

/// <summary>
/// Service for managing teams and team member assignments.
/// </summary>
public class TeamManagementService : ITeamManagementService
{
    private readonly ITeamRepository _teamRepo;
    private readonly ITeamMemberRepository _teamMemberRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;

    public TeamManagementService(
        ITeamRepository teamRepo,
        ITeamMemberRepository teamMemberRepo,
        IUserWorkspaceRepository userWorkspaceRepo)
    {
        _teamRepo = teamRepo;
        _teamMemberRepo = teamMemberRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
    }

    public async Task<Team> CreateTeamAsync(int workspaceId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Team name is required");

        var trimmedName = name.Trim();

        if (!await IsNameUniqueAsync(workspaceId, trimmedName))
            throw new InvalidOperationException($"Team '{trimmedName}' already exists");

        // Use AddAsync which returns the created team with ID
        var team = await _teamRepo.AddAsync(workspaceId, trimmedName, 
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(), 0);

        return team;
    }

    public async Task<Team> UpdateTeamAsync(int teamId, string name, string? description = null)
    {
        var team = await _teamRepo.FindByIdAsync(teamId);
        if (team == null)
            throw new InvalidOperationException("Team not found");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Team name is required");

        var trimmedName = name.Trim();

        if (trimmedName != team.Name && !await IsNameUniqueAsync(team.WorkspaceId, trimmedName, teamId))
            throw new InvalidOperationException($"Team '{trimmedName}' already exists");

        team.Name = trimmedName;
        team.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        team.UpdatedAt = DateTime.UtcNow;

        await _teamRepo.UpdateAsync(team);

        return team;
    }

    public async Task DeleteTeamAsync(int teamId)
    {
        var team = await _teamRepo.FindByIdAsync(teamId);
        if (team == null)
            throw new InvalidOperationException("Team not found");

        await _teamRepo.DeleteAsync(teamId);
    }

    public async Task SyncTeamMembersAsync(int teamId, int workspaceId, List<int> memberUserIds)
    {
        var team = await _teamRepo.FindByIdAsync(teamId);
        if (team == null)
            throw new InvalidOperationException("Team not found");

        if (team.WorkspaceId != workspaceId)
            throw new InvalidOperationException("Team does not belong to workspace");

        // Validate all users are workspace members
        if (!await ValidateMembersAsync(workspaceId, memberUserIds))
            throw new InvalidOperationException("One or more users are not workspace members");

        // Get current members
        var currentMembers = await _teamMemberRepo.ListMembersAsync(teamId);
        var currentUserIds = currentMembers.Select(m => m.Id).ToHashSet();

        var newUserIds = memberUserIds.ToHashSet();

        // Remove members not in new list
        var toRemove = currentUserIds.Except(newUserIds);
        foreach (var userId in toRemove)
        {
            await _teamMemberRepo.RemoveAsync(teamId, userId);
        }

        // Add new members
        var toAdd = newUserIds.Except(currentUserIds);
        foreach (var userId in toAdd)
        {
            await _teamMemberRepo.AddAsync(teamId, userId);
        }
    }

    public async Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeTeamId = null)
    {
        var teams = await _teamRepo.ListForWorkspaceAsync(workspaceId);
        var existing = teams.FirstOrDefault(t => 
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        
        return existing == null || (excludeTeamId.HasValue && existing.Id == excludeTeamId.Value);
    }

    public async Task<bool> ValidateMembersAsync(int workspaceId, List<int> userIds)
    {
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        var validUserIds = memberships.Where(m => m.Accepted).Select(m => m.UserId).ToHashSet();

        return userIds.All(userId => validUserIds.Contains(userId));
    }
}


