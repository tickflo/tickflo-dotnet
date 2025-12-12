using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public ReportsModel(IWorkspaceRepository workspaceRepo)
    {
        _workspaceRepo = workspaceRepo;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
    }
}
