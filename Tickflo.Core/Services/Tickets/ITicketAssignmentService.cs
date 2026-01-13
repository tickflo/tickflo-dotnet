using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles ticket assignment workflows.
/// </summary>
public interface ITicketAssignmentService
{
    /// <summary>
    /// Assigns a ticket to a specific user.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to assign</param>
    /// <param name="assigneeUserId">User to assign to</param>
    /// <param name="assignedByUserId">User performing the assignment</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> AssignToUserAsync(int workspaceId, int ticketId, int assigneeUserId, int assignedByUserId);

    /// <summary>
    /// Assigns a ticket to a team.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to assign</param>
    /// <param name="teamId">Team to assign to</param>
    /// <param name="assignedByUserId">User performing the assignment</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> AssignToTeamAsync(int workspaceId, int ticketId, int teamId, int assignedByUserId);

    /// <summary>
    /// Unassigns a ticket from its current user.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to unassign</param>
    /// <param name="unassignedByUserId">User performing the unassignment</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> UnassignUserAsync(int workspaceId, int ticketId, int unassignedByUserId);

    /// <summary>
    /// Reassigns a ticket from one user to another with optional reason.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to reassign</param>
    /// <param name="newAssigneeUserId">New assignee</param>
    /// <param name="reassignedByUserId">User performing the reassignment</param>
    /// <param name="reason">Optional reason for reassignment</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> ReassignAsync(int workspaceId, int ticketId, int newAssigneeUserId, int reassignedByUserId, string? reason = null);

    /// <summary>
    /// Automatically assigns a ticket based on rules (team round-robin, location default, etc.).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to auto-assign</param>
    /// <param name="triggeredByUserId">User triggering auto-assignment</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> AutoAssignAsync(int workspaceId, int ticketId, int triggeredByUserId);
}
