using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class LocationsNewModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string Address { get; set; } = string.Empty;

    public LocationsNewModel(IWorkspaceRepository workspaceRepo)
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
        TempData["Success"] = $"Location '{Name}' created successfully.";
        return RedirectToPage("/Workspaces/Locations", new { slug });
    }
}
