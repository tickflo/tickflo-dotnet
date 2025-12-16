using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Tickflo.Web.Pages.Workspaces;

public class TicketsNewModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<User> Members { get; private set; } = new();
    [BindProperty]
    public string Subject { get; set; } = string.Empty;
    [BindProperty]
    public string Description { get; set; } = string.Empty;
    [BindProperty]
    public int? ContactId { get; set; }
    [BindProperty]
    public int? AssignedUserId { get; set; }
    [BindProperty]
    public string Priority { get; set; } = "Normal";
    [BindProperty]
    public string? InventoryRef { get; set; }

    public TicketsNewModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IUserRepository users, IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _users = users;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int? contactId = null, [FromServices] IContactRepository contactRepo = null!)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        Contacts = await contactRepo.ListAsync(Workspace.Id);
        // Load workspace members for assignment dropdown
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(Workspace.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var uid2 in userIds)
        {
            var u = await _users.FindByIdAsync(uid2);
            if (u != null) Members.Add(u);
        }
        if (contactId.HasValue)
        {
            ContactId = contactId.Value;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, [FromServices] ITicketRepository ticketRepo, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        if (!ModelState.IsValid)
        {
            return Page();
        }
        var ticket = new Ticket
        {
            WorkspaceId = Workspace.Id,
            ContactId = ContactId,
            Subject = Subject,
            Description = Description,
            Priority = Priority,
            Status = "New",
            InventoryRef = InventoryRef
        };
        if (AssignedUserId.HasValue)
        {
            // Ensure the assignee is a member of the workspace
            var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(Workspace.Id);
            if (memberships.Any(m => m.UserId == AssignedUserId.Value))
            {
                ticket.AssignedUserId = AssignedUserId.Value;
            }
        }
        await ticketRepo.CreateAsync(ticket);
        // Build contact display for nicer rendering
        string? contactDisplay = null;
        string? assignedDisplay = null;
        try {
            var c = Contacts.FirstOrDefault(x => x.Id == ticket.ContactId);
            if (c != null) contactDisplay = $"{c.Name} ({c.Email})";
        } catch { }
        if (ticket.AssignedUserId.HasValue)
        {
            try {
                var au = Members.FirstOrDefault(x => x.Id == ticket.AssignedUserId.Value);
                if (au != null) assignedDisplay = $"{au.Name} ({au.Email})";
            } catch { }
        }
        // Broadcast new ticket to workspace board clients
        await hub.Clients.Group(Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug)).SendCoreAsync("ticketCreated", new object[] {
            new {
                id = ticket.Id,
                subject = ticket.Subject,
                priority = ticket.Priority,
                status = ticket.Status,
                contactId = ticket.ContactId,
                contactDisplay = contactDisplay,
                assignedUserId = ticket.AssignedUserId,
                assignedDisplay = assignedDisplay,
                inventoryRef = ticket.InventoryRef,
                createdAt = ticket.CreatedAt
            }
        });
        TempData["Success"] = $"Ticket '{Subject}' created successfully.";
        return RedirectToPage("/Workspaces/Tickets", new { slug });
    }
}
