namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

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
    public Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tickets for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace to load tickets for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tickets in the workspace</returns>
    public Task<IEnumerable<Ticket>> GetAllTicketsAsync(int workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single ticket by ID.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ticket if found, null otherwise</returns>
    public Task<Ticket?> GetTicketAsync(int workspaceId, int ticketId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for tickets page.
/// </summary>
public class WorkspaceTicketsViewData
{
    /// <summary>
    /// Ticket statuses for the workspace.
    /// </summary>
    public IReadOnlyList<TicketStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Map of status name to color.
    /// </summary>
    public Dictionary<string, string> StatusColorByName { get; set; } = [];

    /// <summary>
    /// Ticket priorities for the workspace.
    /// </summary>
    public IReadOnlyList<TicketPriority> Priorities { get; set; } = [];

    /// <summary>
    /// Map of priority name to color.
    /// </summary>
    public Dictionary<string, string> PriorityColorByName { get; set; } = [];

    /// <summary>
    /// Ticket types for the workspace.
    /// </summary>
    public IReadOnlyList<TicketType> Types { get; set; } = [];

    /// <summary>
    /// Map of type name to color.
    /// </summary>
    public Dictionary<string, string> TypeColorByName { get; set; } = [];

    /// <summary>
    /// Teams in the workspace, indexed by ID.
    /// </summary>
    public Dictionary<int, Team> TeamsById { get; set; } = [];

    /// <summary>
    /// Contacts in the workspace, indexed by ID.
    /// </summary>
    public Dictionary<int, Contact> ContactsById { get; set; } = [];

    /// <summary>
    /// Workspace members, indexed by user ID.
    /// </summary>
    public Dictionary<int, User> UsersById { get; set; } = [];

    /// <summary>
    /// Locations available in the workspace.
    /// </summary>
    public List<Location> LocationOptions { get; set; } = [];

    /// <summary>
    /// Locations, indexed by ID.
    /// </summary>
    public Dictionary<int, Location> LocationsById { get; set; } = [];

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
    public List<int> UserTeamIds { get; set; } = [];
}


