using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class TicketsDetailsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserWorkspaceRoleRepository _roles;
    private readonly IHttpContextAccessor _http;

    public TicketsDetailsModel(IWorkspaceRepository workspaceRepo, ITicketRepository ticketRepo, IContactRepository contactRepo, IUserRepository users, IUserWorkspaceRepository userWorkspaces, IUserWorkspaceRoleRepository roles, IHttpContextAccessor http)
    {
        _workspaceRepo = workspaceRepo;
        _ticketRepo = ticketRepo;
        _contactRepo = contactRepo;
        _users = users;
        _userWorkspaces = userWorkspaces;
        _roles = roles;
        _http = http;
    }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Ticket? Ticket { get; private set; }
    public Contact? Contact { get; private set; }
    public bool IsWorkspaceAdmin { get; private set; }
    public List<User> Members { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? ContactId { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool Mine { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? AssigneeUserId { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    [BindProperty]
    public string? EditSubject { get; set; }
    [BindProperty]
    public string? EditDescription { get; set; }
    [BindProperty]
    public string? EditPriority { get; set; }
    [BindProperty]
    public string? EditStatus { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        Ticket = await _ticketRepo.FindAsync(Workspace.Id, id);
        if (Ticket == null) return NotFound();
        Contact = Ticket.ContactId.HasValue ? await _contactRepo.FindAsync(Workspace.Id, Ticket.ContactId.Value) : null;
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        IsWorkspaceAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
        var memberships = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var uid2 in userIds)
        {
            var u = await _users.FindByIdAsync(uid2);
            if (u != null) Members.Add(u);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostStatusAsync(string slug, int id, string status)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        var t = await _ticketRepo.FindAsync(Workspace.Id, id);
        if (t == null) return NotFound();
        t.Status = status;
        await _ticketRepo.UpdateAsync(t);
        return Redirect($"/workspaces/{slug}/tickets/{id}");
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug, int id, int assignedUserId, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        var memberships = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
        if (!memberships.Any(m => m.UserId == assignedUserId)) return BadRequest("User not in workspace");
        var t = await _ticketRepo.FindAsync(Workspace.Id, id);
        if (t == null) return NotFound();
        t.AssignedUserId = assignedUserId;
        await _ticketRepo.UpdateAsync(t);
        string? display = null;
        if (t.AssignedUserId.HasValue)
        {
            var au = await _users.FindByIdAsync(t.AssignedUserId.Value);
            if (au != null) display = $"{au.Name} ({au.Email})";
        }
        await hub.Clients.Group(Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug)).SendCoreAsync("ticketAssigned", new object[] {
            new {
                id = t.Id,
                assignedUserId = t.AssignedUserId,
                assignedDisplay = display,
                contactId = t.ContactId
            }
        });
        return Redirect($"/workspaces/{slug}/tickets/{id}");
    }

    public async Task<IActionResult> OnPostSaveAsync(string slug, int id, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        var t = await _ticketRepo.FindAsync(Workspace.Id, id);
        if (t == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(EditSubject)) t.Subject = EditSubject!.Trim();
        if (!string.IsNullOrWhiteSpace(EditDescription)) t.Description = EditDescription!.Trim();
        if (!string.IsNullOrWhiteSpace(EditPriority)) t.Priority = EditPriority!;
        if (!string.IsNullOrWhiteSpace(EditStatus)) t.Status = EditStatus!;
        await _ticketRepo.UpdateAsync(t);
        // Broadcast update to workspace clients
        await hub.Clients.Group(Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug)).SendCoreAsync("ticketUpdated", new object[] {
            new {
                id = t.Id,
                subject = t.Subject,
                priority = t.Priority,
                status = t.Status,
                contactId = t.ContactId,
                assignedUserId = t.AssignedUserId,
                inventoryRef = t.InventoryRef,
                updatedAt = DateTime.UtcNow
            }
        });
        TempData["Success"] = "Ticket saved.";
        return Redirect($"/workspaces/{slug}/tickets/{id}");
    }
}
