using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

/// <summary>
/// Service for building client portal view data.
/// Aggregates ticket and metadata information for client access.
/// </summary>
public interface IClientPortalViewService
{
    /// <summary>
    /// Builds view data for a client portal session.
    /// </summary>
    Task<ClientPortalViewData> BuildAsync(Contact contact, int workspaceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of client portal view service.
/// </summary>
public class ClientPortalViewService : IClientPortalViewService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;

    public ClientPortalViewService(
        ITicketRepository ticketRepo,
        IWorkspaceRepository workspaceRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo)
    {
        _ticketRepo = ticketRepo;
        _workspaceRepo = workspaceRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
    }

    public async Task<ClientPortalViewData> BuildAsync(Contact contact, int workspaceId, CancellationToken cancellationToken = default)
    {
        var data = new ClientPortalViewData { Contact = contact };

        // Load workspace
        var workspace = await _workspaceRepo.FindByIdAsync(workspaceId);
        if (workspace == null)
            throw new InvalidOperationException($"Workspace {workspaceId} not found");
        
        data.Workspace = workspace;

        // Load all tickets and filter to contact's tickets
        var allTickets = await _ticketRepo.ListAsync(workspaceId, cancellationToken);
        data.Tickets = allTickets.Where(t => t.ContactId == contact.Id).ToList();

        // Load statuses with fallback defaults
        var statuses = await _statusRepo.ListAsync(workspaceId, cancellationToken);
        var statusList = statuses.Count > 0
            ? statuses
            : new List<TicketStatus>
            {
                new() { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new() { WorkspaceId = workspaceId, Name = "In Progress", Color = "warning", SortOrder = 2, IsClosedState = false },
                new() { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 3, IsClosedState = true },
            };
        data.Statuses = statusList;
        data.StatusColorByName = statusList
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load priorities with fallback defaults
        var priorities = await _priorityRepo.ListAsync(workspaceId, cancellationToken);
        var priorityList = priorities.Count > 0
            ? priorities
            : new List<TicketPriority>
            {
                new() { WorkspaceId = workspaceId, Name = "Low", Color = "success", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "High", Color = "warning", SortOrder = 3 },
                new() { WorkspaceId = workspaceId, Name = "Urgent", Color = "error", SortOrder = 4 },
            };
        data.Priorities = priorityList;
        data.PriorityColorByName = priorityList
            .GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load types with fallback defaults
        var types = await _typeRepo.ListAsync(workspaceId, cancellationToken);
        var typeList = types.Count > 0
            ? types
            : new List<TicketType>
            {
                new() { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 },
            };
        data.Types = typeList;
        data.TypeColorByName = typeList
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        return data;
    }
}

/// <summary>
/// View data for client portal.
/// </summary>
public class ClientPortalViewData
{
    public Contact? Contact { get; set; }
    public Entities.Workspace? Workspace { get; set; }
    public IReadOnlyList<Ticket> Tickets { get; set; } = Array.Empty<Ticket>();
    public IReadOnlyList<TicketStatus> Statuses { get; set; } = Array.Empty<TicketStatus>();
    public Dictionary<string, string> StatusColorByName { get; set; } = new();
    public IReadOnlyList<TicketPriority> Priorities { get; set; } = Array.Empty<TicketPriority>();
    public Dictionary<string, string> PriorityColorByName { get; set; } = new();
    public IReadOnlyList<TicketType> Types { get; set; } = Array.Empty<TicketType>();
    public Dictionary<string, string> TypeColorByName { get; set; } = new();
}
