using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class RolesNewModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    public string RoleName { get; set; } = string.Empty;
    [BindProperty]
    public string Permissions { get; set; } = string.Empty;

    public RolesNewModel(IWorkspaceRepository workspaceRepo)
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
        TempData["Success"] = $"Role '{RoleName}' created successfully.";
        return RedirectToPage("/Workspaces/Roles", new { slug });
    }
}
