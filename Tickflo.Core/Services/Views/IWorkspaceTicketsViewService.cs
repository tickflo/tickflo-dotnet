using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

/// <summary>
/// Service for aggregating and preparing ticket list view data.
/// Consolidates tickets, metadata, and permissions for display.
/// </summary>
public interface IWorkspaceTicketsViewService
{
    /// <summary>
    /// Builds aggregated view data for tickets page.
    /// </summary>
    /// <param name="workspaceId">The workspace to load tickets for</param>
    /// <param name="userId">Current user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>View data containing tickets, statuses, priorities, types, and permissions</returns>
    Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for tickets page.
/// </summary>
public class WorkspaceTicketsViewData
{
    /// <summary>
    /// Ticket statuses for the workspace.
    /// </summary>
    public IReadOnlyList<TicketStatus> Statuses { get; set; } = Array.Empty<TicketStatus>();

    /// <summary>
    /// Map of status name to color.
    /// </summary>
    public Dictionary<string, string> StatusColorByName { get; set; } = new();

    /// <summary>
    /// Ticket priorities for the workspace.
    /// </summary>
    public IReadOnlyList<TicketPriority> Priorities { get; set; } = Array.Empty<TicketPriority>();

    /// <summary>
    /// Map of priority name to color.
    /// </summary>
    public Dictionary<string, string> PriorityColorByName { get; set; } = new();

    /// <summary>
    /// Ticket types for the workspace.
    /// </summary>
    public IReadOnlyList<TicketType> Types { get; set; } = Array.Empty<TicketType>();

    /// <summary>
    /// Map of type name to color.
    /// </summary>
    public Dictionary<string, string> TypeColorByName { get; set; } = new();

    /// <summary>
    /// Teams in the workspace, indexed by ID.
    /// </summary>
    public Dictionary<int, Team> TeamsById { get; set; } = new();

    /// <summary>
    /// Contacts in the workspace, indexed by ID.
    /// </summary>
    public Dictionary<int, Contact> ContactsById { get; set; } = new();

    /// <summary>
    /// Workspace members, indexed by user ID.
    /// </summary>
    public Dictionary<int, User> UsersById { get; set; } = new();

    /// <summary>
    /// Locations available in the workspace.
    /// </summary>
    public List<Location> LocationOptions { get; set; } = new();

    /// <summary>
    /// Locations, indexed by ID.
    /// </summary>
    public Dictionary<int, Location> LocationsById { get; set; } = new();

    /// <summary>
    /// Whether user can create tickets.
    /// </summary>
    public bool CanCreateTickets { get; set; }

    /// <summary>
    /// Whether user can edit tickets.
    /// </summary>
    public bool CanEditTickets { get; set; }

    /// <summary>
    /// Ticket view scope for the user ("all", "mine", or "team").
    /// </summary>
    public string TicketViewScope { get; set; } = "all";

    /// <summary>
    /// Team IDs the user belongs to (when scope is "team").
    /// </summary>
    public List<int> UserTeamIds { get; set; } = new();
}


