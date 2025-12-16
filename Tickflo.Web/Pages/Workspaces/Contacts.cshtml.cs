using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ContactsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IContactRepository _contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = Array.Empty<TicketPriority>();
    public Dictionary<string, string> PriorityColorByName { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    public ContactsModel(IWorkspaceRepository workspaceRepo, IContactRepository contactRepo, ITicketPriorityRepository priorityRepo)
    {
        _workspaceRepo = workspaceRepo;
        _contactRepo = contactRepo;
        _priorityRepo = priorityRepo;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null)
        {
            var all = await _contactRepo.ListAsync(Workspace.Id);
            if (!string.IsNullOrWhiteSpace(Priority))
            {
                Contacts = all.Where(c => string.Equals(c.Priority, Priority, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                Contacts = all;
            }
            Priorities = await _priorityRepo.ListAsync(Workspace.Id);
            PriorityColorByName = Priorities.ToDictionary(p => p.Name, p => string.IsNullOrWhiteSpace(p.Color) ? "neutral" : p.Color);
        }
    }
}
