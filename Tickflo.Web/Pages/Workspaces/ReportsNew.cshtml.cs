using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportsNewModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    public string Title { get; set; } = string.Empty;
    [BindProperty]
    public string Type { get; set; } = "Summary";

    public ReportsNewModel(IWorkspaceRepository workspaceRepo)
    {
        _workspaceRepo = workspaceRepo;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (!ModelState.IsValid)
        {
            return Page();
        }
        TempData["Success"] = $"Report '{Title}' created successfully.";
        return RedirectToPage("/Workspaces/Reports", new { slug });
    }
}
