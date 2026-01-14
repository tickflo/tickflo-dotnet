using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Views;

namespace Tickflo.Web.Pages;

/// <summary>
/// Client portal page model.
/// Allows clients to view and create tickets associated with their contact.
/// </summary>
[AllowAnonymous]
public class ClientPortalModel : PageModel
{
    private readonly IContactRepository _contactRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IClientPortalViewService _viewService;
    private readonly ITicketCommentService _commentService;

    public ClientPortalModel(
        IContactRepository contactRepo,
        ITicketRepository ticketRepo,
        IClientPortalViewService viewService,
        ITicketCommentService commentService)
    {
        _contactRepo = contactRepo;
        _ticketRepo = ticketRepo;
        _viewService = viewService;
        _commentService = commentService;
    }

    // View Data Properties
    public Contact? Contact { get; private set; }
    public Workspace? Workspace { get; private set; }
    public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();
    public IReadOnlyList<TicketStatus> Statuses { get; private set; } = Array.Empty<TicketStatus>();
    public Dictionary<string, string> StatusColorByName { get; private set; } = new();
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = Array.Empty<TicketPriority>();
    public Dictionary<string, string> PriorityColorByName { get; private set; } = new();
    public IReadOnlyList<TicketType> Types { get; private set; } = Array.Empty<TicketType>();
    public Ticket? SelectedTicket { get; private set; }
    public IReadOnlyList<TicketComment> TicketComments { get; private set; } = Array.Empty<TicketComment>();

    // Form Binding Properties
    [BindProperty(SupportsGet = true)]
    public string AccessToken { get; set; } = string.Empty;

    [BindProperty]
    public int? SelectedTicketId { get; set; }

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? TicketType { get; set; }

    [BindProperty]
    public string? Priority { get; set; }

    [BindProperty]
    public string? NewCommentContent { get; set; }

    /// <summary>
    /// Handles GET requests to display the client portal.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(string token, CancellationToken cancellationToken = default)
    {
        AccessToken = token;

        var contact = await _contactRepo.FindByAccessTokenAsync(token, cancellationToken);
        if (contact == null)
            return NotFound();

        return await LoadPortalDataAsync(contact, cancellationToken);
    }

    /// <summary>
    /// Handles POST requests to create new tickets or add comments.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(string token, string? action = "createTicket", CancellationToken cancellationToken = default)
    {
        AccessToken = token;

        var contact = await _contactRepo.FindByAccessTokenAsync(token, cancellationToken);
        if (contact == null)
            return Unauthorized();

        if (action == "addComment" && SelectedTicketId.HasValue)
        {
            return await HandleAddCommentAsync(contact, cancellationToken);
        }
        else if (action == "updateTicket" && SelectedTicketId.HasValue)
        {
            return await HandleUpdateTicketAsync(contact, cancellationToken);
        }
        else
        {
            return await HandleCreateTicketAsync(contact, cancellationToken);
        }
    }

    /// <summary>
    /// Handles creating a new ticket.
    /// </summary>
    private async Task<IActionResult> HandleCreateTicketAsync(Contact contact, CancellationToken cancellationToken)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(Subject))
        {
            ModelState.AddModelError("Subject", "Subject is required");
            return await LoadPortalDataAsync(contact, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError("Description", "Description is required");
            return await LoadPortalDataAsync(contact, cancellationToken);
        }

        // Create ticket with contact locked association
        var ticket = new Ticket
        {
            WorkspaceId = contact.WorkspaceId,
            ContactId = contact.Id, // Locked to contact - cannot be changed
            Subject = Subject.Trim(),
            Description = Description.Trim(),
            Type = TicketType ?? "Standard",
            Priority = Priority ?? "Normal",
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };

        await _ticketRepo.CreateAsync(ticket, cancellationToken);

        // Clear form and reload data
        Subject = string.Empty;
        Description = string.Empty;
        TicketType = null;
        Priority = null;

        return await LoadPortalDataAsync(contact, cancellationToken);
    }

    /// <summary>
    /// Handles updating an existing open ticket.
    /// </summary>
    private async Task<IActionResult> HandleUpdateTicketAsync(Contact contact, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepo.FindAsync(contact.WorkspaceId, SelectedTicketId!.Value, cancellationToken);
        if (ticket == null || ticket.ContactId != contact.Id)
            return Unauthorized();

        // Only allow editing open tickets (not "Closed" status)
        if (!string.IsNullOrEmpty(ticket.Status) && ticket.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Cannot edit closed tickets");
            return await LoadPortalDataAsync(contact, cancellationToken);
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(Subject))
        {
            ModelState.AddModelError("Subject", "Subject is required");
            return await LoadTicketDetailsAsync(contact, SelectedTicketId.Value, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ModelState.AddModelError("Description", "Description is required");
            return await LoadTicketDetailsAsync(contact, SelectedTicketId.Value, cancellationToken);
        }

        // Update ticket
        ticket.Subject = Subject.Trim();
        ticket.Description = Description.Trim();
        ticket.Type = TicketType ?? ticket.Type;
        ticket.Priority = Priority ?? ticket.Priority;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket, cancellationToken);

        // Clear form and reload data
        Subject = string.Empty;
        Description = string.Empty;
        TicketType = null;
        Priority = null;

        return await LoadPortalDataAsync(contact, cancellationToken);
    }

    /// <summary>
    /// Handles adding a comment to a ticket.
    /// </summary>
    private async Task<IActionResult> HandleAddCommentAsync(Contact contact, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepo.FindAsync(contact.WorkspaceId, SelectedTicketId!.Value, cancellationToken);
        if (ticket == null || ticket.ContactId != contact.Id)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(NewCommentContent))
        {
            ModelState.AddModelError("NewCommentContent", "Comment cannot be empty");
            return await LoadTicketDetailsAsync(contact, SelectedTicketId.Value, cancellationToken);
        }

        // Use the new client comment method
        await _commentService.AddClientCommentAsync(
            contact.WorkspaceId,
            ticket.Id,
            contact.Id,
            NewCommentContent.Trim(),
            cancellationToken);

        NewCommentContent = string.Empty;
        return await LoadTicketDetailsAsync(contact, SelectedTicketId.Value, cancellationToken);
    }

    /// <summary>
    /// Handles GET request with ticket ID to view ticket details.
    /// </summary>
    public async Task<IActionResult> OnGetViewTicketAsync(string token, int ticketId, CancellationToken cancellationToken = default)
    {
        AccessToken = token;

        var contact = await _contactRepo.FindByAccessTokenAsync(token, cancellationToken);
        if (contact == null)
            return NotFound();

        return await LoadTicketDetailsAsync(contact, ticketId, cancellationToken);
    }

    /// <summary>
    /// Loads and populates all necessary data for the portal view.
    /// </summary>
    private async Task<IActionResult> LoadPortalDataAsync(Contact contact, CancellationToken cancellationToken)
    {
        try
        {
            Contact = contact;

            var viewData = await _viewService.BuildAsync(contact, contact.WorkspaceId, cancellationToken);
            if (viewData?.Workspace == null)
                return NotFound();

            Workspace = viewData.Workspace;
            Tickets = viewData.Tickets;
            Statuses = viewData.Statuses;
            StatusColorByName = viewData.StatusColorByName;
            Priorities = viewData.Priorities;
            PriorityColorByName = viewData.PriorityColorByName;
            Types = viewData.Types;

            return Page();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Loads ticket details and comments for viewing/editing.
    /// </summary>
    private async Task<IActionResult> LoadTicketDetailsAsync(Contact contact, int ticketId, CancellationToken cancellationToken)
    {
        try
        {
            Contact = contact;

            var viewData = await _viewService.BuildAsync(contact, contact.WorkspaceId, cancellationToken);
            if (viewData?.Workspace == null)
                return NotFound();

            Workspace = viewData.Workspace;
            Tickets = viewData.Tickets;
            Statuses = viewData.Statuses;
            StatusColorByName = viewData.StatusColorByName;
            Priorities = viewData.Priorities;
            PriorityColorByName = viewData.PriorityColorByName;
            Types = viewData.Types;

            // Load selected ticket
            var ticket = await _ticketRepo.FindAsync(contact.WorkspaceId, ticketId, cancellationToken);
            if (ticket == null || ticket.ContactId != contact.Id)
                return NotFound();

            SelectedTicketId = ticketId;
            SelectedTicket = ticket;

            // Pre-populate form fields for editing
            Subject = ticket.Subject;
            Description = ticket.Description;
            TicketType = ticket.Type;
            Priority = ticket.Priority;

            // Load comments visible to client
            TicketComments = await _commentService.GetCommentsAsync(
                contact.WorkspaceId,
                ticketId,
                isClientView: true,
                cancellationToken);

            return Page();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}

