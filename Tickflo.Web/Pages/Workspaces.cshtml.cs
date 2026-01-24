namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class WorkspacesModel(IWorkspaceService workspaceService) : PageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    public List<UserWorkspace> Workspaces { get; set; } = [];
    [BindProperty]
    public string NewWorkspaceName { get; set; } = string.Empty;
    public async Task<IActionResult> OnGetAsync([FromServices] IAppContext appContext)
    {
        var user = appContext.CurrentUser;
        if (user == null)
        {
            return this.Unauthorized();
        }

        this.Workspaces = await this.workspaceService.GetUserWorkspacesAsync(user.Id);

        if (this.Workspaces.Count == 1)
        {
            var singleWorkspace = this.Workspaces[0];
            return this.Redirect($"/workspaces/{singleWorkspace.Workspace.Slug}");
        }

        return this.Page();
    }
}
