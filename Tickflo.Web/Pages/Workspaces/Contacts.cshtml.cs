using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ContactsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IContactRepository _contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = Array.Empty<TicketPriority>();
    public Dictionary<string, string> PriorityColorByName { get; private set; } = new();
    public bool CanCreateContacts { get; private set; }
    public bool CanEditContacts { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public ContactsModel(
        IWorkspaceRepository workspaceRepo,
        IContactRepository contactRepo,
        ITicketPriorityRepository priorityRepo,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService)
    {
        _workspaceRepo = workspaceRepo;
        _contactRepo = contactRepo;
        _priorityRepo = priorityRepo;
        _currentUserService = currentUserService;
        _workspaceAccessService = workspaceAccessService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!_currentUserService.TryGetUserId(User, out var uid)) return Forbid();

        // Use service to get permissions
        var permissions = await _workspaceAccessService.GetUserPermissionsAsync(Workspace.Id, uid);
        if (permissions.TryGetValue("contacts", out var cp))
        {
            CanCreateContacts = cp.CanCreate;
            CanEditContacts = cp.CanEdit;
        }
        else
        {
            return Forbid();
        }

        var all = await _contactRepo.ListAsync(Workspace.Id);
        IEnumerable<Contact> filtered = all;
        
        if (!string.IsNullOrWhiteSpace(Priority))
        {
            filtered = filtered.Where(c => string.Equals(c.Priority, Priority, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var q = Query.Trim();
            filtered = filtered.Where(c =>
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Email) && c.Email.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Company) && c.Company.Contains(q, StringComparison.OrdinalIgnoreCase))
            );
        }
        
        Contacts = filtered.ToList();
        Priorities = await _priorityRepo.ListAsync(Workspace.Id);
        PriorityColorByName = Priorities.ToDictionary(p => p.Name, p => string.IsNullOrWhiteSpace(p.Color) ? "neutral" : p.Color);
        return Page();
    }
}
