using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
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

    public ClientPortalModel(
        IContactRepository contactRepo,
        ITicketRepository ticketRepo,
        IClientPortalViewService viewService)
    {
        _contactRepo = contactRepo;
        _ticketRepo = ticketRepo;
        _viewService = viewService;
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

    // Form Binding Properties
    [BindProperty(SupportsGet = true)]
    public string AccessToken { get; set; } = string.Empty;

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? TicketType { get; set; }

    [BindProperty]
    public string? Priority { get; set; }

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
    /// Handles POST requests to create new tickets.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(string token, CancellationToken cancellationToken = default)
    {
        AccessToken = token;

        var contact = await _contactRepo.FindByAccessTokenAsync(token, cancellationToken);
        if (contact == null)
            return Unauthorized();

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
}

