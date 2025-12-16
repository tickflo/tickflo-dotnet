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
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    public string Subject { get; set; } = string.Empty;
    [BindProperty]
    public string Description { get; set; } = string.Empty;
    [BindProperty]
    public int ContactId { get; set; }
    [BindProperty]
    public string Priority { get; set; } = "Normal";
    [BindProperty]
    public string? InventoryRef { get; set; }

    public TicketsNewModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int? contactId = null)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        if (contactId.HasValue)
        {
            ContactId = contactId.Value;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, [FromServices] ITicketRepository ticketRepo)
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
        await ticketRepo.CreateAsync(ticket);
        TempData["Success"] = $"Ticket '{Subject}' created successfully.";
        return RedirectToPage("/Workspaces/Tickets", new { slug });
    }
}
