using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles the business workflow of assigning tickets to users and teams.
/// </summary>
public class TicketAssignmentService : ITicketAssignmentService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ITeamMemberRepository _teamMemberRepo;

    public TicketAssignmentService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ITeamRepository teamRepo,
        ITeamMemberRepository teamMemberRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _teamRepo = teamRepo;
        _teamMemberRepo = teamMemberRepo;
    }

    /// <summary>
    /// Assigns a ticket to a specific user.
    /// </summary>
    public async Task<Ticket> AssignToUserAsync(
        int workspaceId, 
        int ticketId, 
        int assigneeUserId, 
        int assignedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Business rule: Validate user has access to workspace
        var userWorkspace = await _userWorkspaceRepo.FindAsync(assigneeUserId, workspaceId);
        if (userWorkspace == null)
            throw new InvalidOperationException("User does not have access to this workspace");

        if (!userWorkspace.Accepted)
            throw new InvalidOperationException("User has not accepted workspace invitation");

        var previousAssignee = ticket.AssignedUserId;

        ticket.AssignedUserId = assigneeUserId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        // Log assignment change
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = assignedByUserId,
            Action = "assigned",
            Note = $"Ticket assigned to user {assigneeUserId}" + 
                   (previousAssignee.HasValue ? $" (was user {previousAssignee.Value})" : "")
        });

        // Could add: Send notification to assignee, update team assignment, etc.

        return ticket;
    }

    /// <summary>
    /// Assigns a ticket to a team.
    /// </summary>
    public async Task<Ticket> AssignToTeamAsync(
        int workspaceId, 
        int ticketId, 
        int teamId, 
        int assignedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Business rule: Validate team belongs to workspace
        var team = await _teamRepo.FindByIdAsync(teamId);
        if (team == null)
            throw new InvalidOperationException("Team not found");

        if (team.WorkspaceId != workspaceId)
            throw new InvalidOperationException("Team does not belong to this workspace");

        var previousTeam = ticket.AssignedTeamId;

        ticket.AssignedTeamId = teamId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        // Log team assignment
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = assignedByUserId,
            Action = "team_assigned",
            Note = $"Ticket assigned to team {team.Name}" +
                   (previousTeam.HasValue ? $" (was team {previousTeam.Value})" : "")
        });

        // Could add: Notify team members, round-robin assign within team, etc.

        return ticket;
    }

    /// <summary>
    /// Unassigns a ticket from its current user assignee.
    /// </summary>
    public async Task<Ticket> UnassignUserAsync(
        int workspaceId, 
        int ticketId, 
        int unassignedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        if (!ticket.AssignedUserId.HasValue)
            return ticket; // Already unassigned

        var previousAssignee = ticket.AssignedUserId.Value;

        ticket.AssignedUserId = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = unassignedByUserId,
            Action = "unassigned",
            Note = $"Ticket unassigned from user {previousAssignee}"
        });

        return ticket;
    }

    /// <summary>
    /// Reassigns a ticket from one user to another.
    /// </summary>
    public async Task<Ticket> ReassignAsync(
        int workspaceId, 
        int ticketId, 
        int newAssigneeUserId, 
        int reassignedByUserId, 
        string? reason = null)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        var previousAssignee = ticket.AssignedUserId;

        // Use the assign method for validation
        ticket = await AssignToUserAsync(workspaceId, ticketId, newAssigneeUserId, reassignedByUserId);

        if (!string.IsNullOrWhiteSpace(reason))
        {
            await _historyRepo.CreateAsync(new TicketHistory
            {
                WorkspaceId = workspaceId,
                TicketId = ticketId,
                CreatedByUserId = reassignedByUserId,
                Action = "reassignment_note",
                Note = $"Reassignment reason: {reason}"
            });
        }

        return ticket;
    }

    /// <summary>
    /// Automatically assigns a ticket based on team round-robin or location default.
    /// </summary>
    public async Task<Ticket> AutoAssignAsync(
        int workspaceId, 
        int ticketId, 
        int triggeredByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Business rule: Try team-based assignment first
        if (ticket.AssignedTeamId.HasValue)
        {
            var teamMembers = await _teamMemberRepo.ListMembersAsync(ticket.AssignedTeamId.Value);
            if (teamMembers.Any())
            {
                // Simple round-robin: assign to first available member
                // Could be enhanced with load balancing, availability checks, etc.
                var assignee = teamMembers.First();
                return await AssignToUserAsync(workspaceId, ticketId, assignee.Id, triggeredByUserId);
            }
        }

        // Business rule: Fall back to location default if no team assignment
        // (This logic could be moved here from TicketManagementService)

        return ticket;
    }
}
