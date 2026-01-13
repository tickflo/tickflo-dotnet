using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Service for managing ticket lifecycle operations including creation, updates, and history tracking.
/// </summary>
public interface ITicketManagementService
{
    /// <summary>
    /// Creates a new ticket with the provided details.
    /// </summary>
    /// <param name="request">Ticket creation request</param>
    /// <returns>The created ticket</returns>
    Task<Ticket> CreateTicketAsync(CreateTicketRequest request);

    /// <summary>
    /// Updates an existing ticket and logs changes to history.
    /// </summary>
    /// <param name="request">Ticket update request</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> UpdateTicketAsync(UpdateTicketRequest request);

    /// <summary>
    /// Validates ticket assignment permissions.
    /// </summary>
    /// <param name="userId">User to assign</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>True if assignment is valid</returns>
    Task<bool> ValidateUserAssignmentAsync(int userId, int workspaceId);

    /// <summary>
    /// Validates team assignment permissions.
    /// </summary>
    /// <param name="teamId">Team to assign</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>True if assignment is valid</returns>
    Task<bool> ValidateTeamAssignmentAsync(int teamId, int workspaceId);

    /// <summary>
    /// Resolves the default assignee for a location.
    /// </summary>
    /// <param name="locationId">The location</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>User ID if a valid default assignee exists</returns>
    Task<int?> ResolveDefaultAssigneeAsync(int locationId, int workspaceId);

    /// <summary>
    /// Checks if a user can access a ticket based on scope rules.
    /// </summary>
    /// <param name="ticket">The ticket to check</param>
    /// <param name="userId">User requesting access</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="isAdmin">Whether user is admin</param>
    /// <returns>True if user can access ticket</returns>
    Task<bool> CanUserAccessTicketAsync(Ticket ticket, int userId, int workspaceId, bool isAdmin);

    /// <summary>
    /// Generates a display name for an assigned user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Formatted display name</returns>
    Task<string?> GetAssigneeDisplayNameAsync(int userId);

    /// <summary>
    /// Generates an inventory summary for SignalR broadcast.
    /// </summary>
    /// <param name="inventories">Ticket inventories</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>Inventory summary and details</returns>
    Task<(string summary, string details)> GenerateInventorySummaryAsync(
        List<TicketInventory> inventories, 
        int workspaceId);
}

/// <summary>
/// Request to create a new ticket.
/// </summary>
public class CreateTicketRequest
{
    public int WorkspaceId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Standard";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "New";
    public int? ContactId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public int? LocationId { get; set; }
    public List<TicketInventory> Inventories { get; set; } = new();
}

/// <summary>
/// Request to update an existing ticket.
/// </summary>
public class UpdateTicketRequest
{
    public int TicketId { get; set; }
    public int WorkspaceId { get; set; }
    public int UpdatedByUserId { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? ContactId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public int? LocationId { get; set; }
    public List<TicketInventory>? Inventories { get; set; }
}


