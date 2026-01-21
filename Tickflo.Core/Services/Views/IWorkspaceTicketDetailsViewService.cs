namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;
using InventoryEntity = Entities.Inventory;

/// <summary>
/// Service for aggregating and preparing ticket details view data.
/// Consolidates metadata, permissions, and scope enforcement for display.
/// </summary>
public interface IWorkspaceTicketDetailsViewService
{
    /// <summary>
    /// Builds aggregated view data for ticket details page.
    /// Performs permission checks and scope enforcement.
    /// </summary>
    /// <param name="workspaceId">The workspace</param>
    /// <param name="ticketId">The ticket ID (0 for new ticket)</param>
    /// <param name="userId">Current user ID</param>
    /// <param name="locationId">Location filter ID if provided</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>View data containing ticket, metadata, and permissions; null if access denied or ticket not found</returns>
    public Task<WorkspaceTicketDetailsViewData?> BuildAsync(
        int workspaceId,
        int ticketId,
        int userId,
        int? locationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated view data for ticket details page.
/// </summary>
public class WorkspaceTicketDetailsViewData
{
    /// <summary>
    /// The ticket being viewed/edited (null for new ticket).
    /// </summary>
    public Ticket? Ticket { get; set; }

    /// <summary>
    /// Contact for the ticket if available.
    /// </summary>
    public Contact? Contact { get; set; }

    /// <summary>
    /// All contacts for dropdown/selection.
    /// </summary>
    public IReadOnlyList<Contact> Contacts { get; set; } = [];

    /// <summary>
    /// Ticket statuses with fallback defaults.
    /// </summary>
    public IReadOnlyList<TicketStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Map of status name to color.
    /// </summary>
    public Dictionary<string, string> StatusColorByName { get; set; } = [];

    /// <summary>
    /// Ticket priorities with fallback defaults.
    /// </summary>
    public IReadOnlyList<TicketPriority> Priorities { get; set; } = [];

    /// <summary>
    /// Map of priority name to color.
    /// </summary>
    public Dictionary<string, string> PriorityColorByName { get; set; } = [];

    /// <summary>
    /// Ticket types with fallback defaults.
    /// </summary>
    public IReadOnlyList<TicketType> Types { get; set; } = [];

    /// <summary>
    /// Map of type name to color.
    /// </summary>
    public Dictionary<string, string> TypeColorByName { get; set; } = [];

    /// <summary>
    /// Ticket history for existing tickets.
    /// </summary>
    public IReadOnlyList<TicketHistory> History { get; set; } = [];

    /// <summary>
    /// Workspace members for assignee selection.
    /// </summary>
    public List<User> Members { get; set; } = [];

    /// <summary>
    /// Teams in the workspace.
    /// </summary>
    public List<Team> Teams { get; set; } = [];

    /// <summary>
    /// InventoryEntity items available for reference.
    /// </summary>
    public List<InventoryEntity> InventoryItems { get; set; } = [];

    /// <summary>
    /// Location options for filtering.
    /// </summary>
    public List<Location> LocationOptions { get; set; } = [];

    /// <summary>
    /// Whether user can view tickets.
    /// </summary>
    public bool CanViewTickets { get; set; }

    /// <summary>
    /// Whether user can edit tickets.
    /// </summary>
    public bool CanEditTickets { get; set; }

    /// <summary>
    /// Whether user can create tickets.
    /// </summary>
    public bool CanCreateTickets { get; set; }

    /// <summary>
    /// Whether user is workspace admin.
    /// </summary>
    public bool IsWorkspaceAdmin { get; set; }

    /// <summary>
    /// Ticket view scope: "all", "mine", or "team".
    /// </summary>
    public string TicketViewScope { get; set; } = "all";
}




