using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ContactsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IContactRepository _contactRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();

    public ContactsModel(IWorkspaceRepository workspaceRepo, IContactRepository contactRepo)
    {
        _workspaceRepo = workspaceRepo;
        _contactRepo = contactRepo;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null)
        {
            Contacts = await _contactRepo.ListAsync(Workspace.Id);
        }
    }
}
