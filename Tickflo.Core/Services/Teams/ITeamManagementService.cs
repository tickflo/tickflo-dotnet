namespace Tickflo.Core.Services.Teams;

using Tickflo.Core.Entities;

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


