using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using System.Linq;

namespace Tickflo.Web.Pages.Workspaces;

public class TicketsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _users;
    private readonly IHttpContextAccessor _http;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();
    public Dictionary<int, Contact> ContactsById { get; private set; } = new();
    public Dictionary<int, User> UsersById { get; private set; } = new();

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
    public string? Query { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;
    public int Total { get; private set; }
    public int MyCount { get; private set; }

    public TicketsModel(IWorkspaceRepository workspaceRepo, ITicketRepository ticketRepo, IContactRepository contactRepo, IUserWorkspaceRepository userWorkspaces, IUserRepository users, IHttpContextAccessor http)
    {
        _workspaceRepo = workspaceRepo;
        _ticketRepo = ticketRepo;
        _contactRepo = contactRepo;
        _userWorkspaces = userWorkspaces;
        _users = users;
        _http = http;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null)
        {
            var all = await _ticketRepo.ListAsync(Workspace.Id);
            var filtered = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(Status)) filtered = filtered.Where(t => t.Status == Status);
            if (!string.IsNullOrWhiteSpace(Priority)) filtered = filtered.Where(t => t.Priority == Priority);
            if (ContactId.HasValue && ContactId.Value > 0) filtered = filtered.Where(t => t.ContactId == ContactId.Value);
            if (!string.IsNullOrWhiteSpace(Query))
            {
                var q = Query.Trim();
                filtered = filtered.Where(t => (t.Subject?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                                            || (t.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int currentUid = 0;
            if (int.TryParse(uidStr, out var uidTmp)) currentUid = uidTmp;
            MyCount = currentUid > 0 ? all.Count(t => t.AssignedUserId == currentUid) : 0;

            if (Mine && currentUid > 0)
            {
                filtered = filtered.Where(t => t.AssignedUserId == currentUid);
            }
            if (AssigneeUserId.HasValue)
            {
                if (AssigneeUserId.Value == -1)
                {
                    filtered = filtered.Where(t => t.AssignedUserId == null);
                }
                else if (AssigneeUserId.Value > 0)
                {
                    filtered = filtered.Where(t => t.AssignedUserId == AssigneeUserId.Value);
                }
            }
            Total = filtered.Count();
            var size = PageSize <= 0 ? 25 : Math.Min(PageSize, 200);
            var page = PageNumber <= 0 ? 1 : PageNumber;
            Tickets = filtered.Skip((page - 1) * size).Take(size).ToList();

            // Load contacts for display (name/email)
            var contacts = await _contactRepo.ListAsync(Workspace.Id);
            ContactsById = contacts.ToDictionary(c => c.Id, c => c);

            // Load workspace members for assignee display/filter
            var memberships = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
            var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
            var users = new List<User>();
            foreach (var id in userIds)
            {
                var u = await _users.FindByIdAsync(id);
                if (u != null) users.Add(u);
            }
            UsersById = users.ToDictionary(u => u.Id, u => u);
        }
    }
}
