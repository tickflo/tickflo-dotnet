namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of assigning tickets to users and teams.
/// </summary>
public class TicketAssignmentService(
    ITicketRepository ticketRepository,
    ITicketHistoryRepository historyRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepo) : ITicketAssignmentService
{
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketHistoryRepository historyRepository = historyRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMemberRepo;

    /// <summary>
    /// Assigns a ticket to a specific user.
    /// </summary>
    public async Task<Ticket> AssignToUserAsync(
        int workspaceId,
        int ticketId,
        int assigneeUserId,
        int assignedByUserId)
    {
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        // Business rule: Validate user has access to workspace
        var userWorkspace = await this.userWorkspaceRepository.FindAsync(assigneeUserId, workspaceId) ?? throw new InvalidOperationException("User does not have access to this workspace");

        if (!userWorkspace.Accepted)
        {
            throw new InvalidOperationException("User has not accepted workspace invitation");
        }

        var previousAssignee = ticket.AssignedUserId;

        ticket.AssignedUserId = assigneeUserId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        // Log assignment change
        await this.historyRepository.CreateAsync(new TicketHistory
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
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        // Business rule: Validate team belongs to workspace
        var team = await this.teamRepository.FindByIdAsync(teamId) ?? throw new InvalidOperationException("Team not found");

        if (team.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException("Team does not belong to this workspace");
        }

        var previousTeam = ticket.AssignedTeamId;

        ticket.AssignedTeamId = teamId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        // Log team assignment
        await this.historyRepository.CreateAsync(new TicketHistory
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
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        if (!ticket.AssignedUserId.HasValue)
        {
            return ticket; // Already unassigned
        }

        var previousAssignee = ticket.AssignedUserId.Value;

        ticket.AssignedUserId = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        await this.historyRepository.CreateAsync(new TicketHistory
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
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        var previousAssignee = ticket.AssignedUserId;

        // Use the assign method for validation
        ticket = await this.AssignToUserAsync(workspaceId, ticketId, newAssigneeUserId, reassignedByUserId);

        if (!string.IsNullOrWhiteSpace(reason))
        {
            await this.historyRepository.CreateAsync(new TicketHistory
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
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        // Business rule: Try team-based assignment first
        if (ticket.AssignedTeamId.HasValue)
        {
            var teamMembers = await this.teamMemberRepository.ListMembersAsync(ticket.AssignedTeamId.Value);
            if (teamMembers.Count != 0)
            {
                // Simple round-robin: assign to first available member
                // Could be enhanced with load balancing, availability checks, etc.
                var assignee = teamMembers.First();
                return await this.AssignToUserAsync(workspaceId, ticketId, assignee.Id, triggeredByUserId);
            }
        }

        // Business rule: Fall back to location default if no team assignment
        // (This logic could be moved here from TicketManagementService)

        return ticket;
    }
}
