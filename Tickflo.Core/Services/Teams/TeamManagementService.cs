namespace Tickflo.Core.Services.Teams;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service for managing teams and team member assignments.
/// </summary>

/// <summary>
/// Service for managing teams and team member assignments.
/// </summary>
public interface ITeamManagementService
{
    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Team name</param>
    /// <param name="description">Team description</param>
    /// <returns>Created team</returns>
    public Task<Team> CreateTeamAsync(int workspaceId, string name, string? description = null);

    /// <summary>
    /// Updates an existing team.
    /// </summary>
    /// <param name="teamId">Team to update</param>
    /// <param name="name">New name</param>
    /// <param name="description">New description</param>
    /// <returns>Updated team</returns>
    public Task<Team> UpdateTeamAsync(int teamId, string name, string? description = null);

    /// <summary>
    /// Deletes a team.
    /// </summary>
    /// <param name="teamId">Team to delete</param>
    public Task DeleteTeamAsync(int teamId);

    /// <summary>
    /// Synchronizes team member assignments (adds new, removes old).
    /// </summary>
    /// <param name="teamId">Team to update</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="memberUserIds">Current member user IDs</param>
    public Task SyncTeamMembersAsync(int teamId, int workspaceId, List<int> memberUserIds);

    /// <summary>
    /// Validates team name uniqueness within a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Team name to check</param>
    /// <param name="excludeTeamId">Optional team ID to exclude</param>
    /// <returns>True if name is unique</returns>
    public Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeTeamId = null);

    /// <summary>
    /// Validates that all user IDs are members of the workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="userIds">User IDs to validate</param>
    /// <returns>True if all users are valid members</returns>
    public Task<bool> ValidateMembersAsync(int workspaceId, List<int> userIds);
}

public class TeamManagementService(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository,
    IUserWorkspaceRepository userWorkspaceRepository) : ITeamManagementService
{
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMemberRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;

    public async Task<Team> CreateTeamAsync(int workspaceId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Team name is required");
        }

        var trimmedName = name.Trim();

        if (!await this.IsNameUniqueAsync(workspaceId, trimmedName))
        {
            throw new InvalidOperationException($"Team '{trimmedName}' already exists");
        }

        // Use AddAsync which returns the created team with ID
        var team = await this.teamRepository.AddAsync(workspaceId, trimmedName,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(), 0);

        return team;
    }

    public async Task<Team> UpdateTeamAsync(int teamId, string name, string? description = null)
    {
        var team = await this.teamRepository.FindByIdAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Team name is required");
        }

        var trimmedName = name.Trim();

        if (trimmedName != team.Name && !await this.IsNameUniqueAsync(team.WorkspaceId, trimmedName, teamId))
        {
            throw new InvalidOperationException($"Team '{trimmedName}' already exists");
        }

        team.Name = trimmedName;
        team.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        team.UpdatedAt = DateTime.UtcNow;

        await this.teamRepository.UpdateAsync(team);

        return team;
    }

    public async Task DeleteTeamAsync(int teamId)
    {
        var team = await this.teamRepository.FindByIdAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        await this.teamRepository.DeleteAsync(teamId);
    }

    public async Task SyncTeamMembersAsync(int teamId, int workspaceId, List<int> memberUserIds)
    {
        var team = await this.teamRepository.FindByIdAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        if (team.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException("Team does not belong to workspace");
        }

        // Validate all users are workspace members
        if (!await this.ValidateMembersAsync(workspaceId, memberUserIds))
        {
            throw new InvalidOperationException("One or more users are not workspace members");
        }

        // Get current members
        var currentMembers = await this.teamMemberRepository.ListMembersAsync(teamId);
        var currentUserIds = currentMembers.Select(m => m.Id).ToHashSet();

        var newUserIds = memberUserIds.ToHashSet();

        // Remove members not in new list
        var toRemove = currentUserIds.Except(newUserIds);
        foreach (var userId in toRemove)
        {
            await this.teamMemberRepository.RemoveAsync(teamId, userId);
        }

        // Add new members
        var toAdd = newUserIds.Except(currentUserIds);
        foreach (var userId in toAdd)
        {
            await this.teamMemberRepository.AddAsync(teamId, userId);
        }
    }

    public async Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeTeamId = null)
    {
        var teams = await this.teamRepository.ListForWorkspaceAsync(workspaceId);
        var existing = teams.FirstOrDefault(t =>
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

        return existing == null || (excludeTeamId.HasValue && existing.Id == excludeTeamId.Value);
    }

    public async Task<bool> ValidateMembersAsync(int workspaceId, List<int> userIds)
    {
        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        var validUserIds = memberships.Where(m => m.Accepted).Select(m => m.UserId).ToHashSet();

        return userIds.All(validUserIds.Contains);
    }
}


