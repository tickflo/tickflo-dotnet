namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

[ApiController]
public class WorkspaceInviteController(
    IWorkspaceRepository workspaceRepository,
    IUserWorkspaceRepository userWorkspaceRepository
    ) : ControllerBase
{
    #region Constants
    private const string InvalidTokenError = "Invalid or expired token.";
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;

    [HttpGet("workspaces/{slug}/accept")]
    [Authorize]
    public async Task<IActionResult> Accept([FromServices] IAppContext appContext, string slug)
    {
        var user = appContext.CurrentUser;
        if (user == null)
        {
            return this.Unauthorized();
        }

        if (!ValidateInput(slug))
        {
            return this.BadRequest(InvalidTokenError);
        }

        var workspace = await this.workspaceRepository.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        var userWorkspace = await this.userWorkspaceRepository.FindAsync(user.Id, workspace.Id);
        if (userWorkspace == null)
        {
            return this.NotFound();
        }

        await this.AcceptWorkspaceInviteAsync(userWorkspace, user);
        return this.Redirect($"/workspaces/{workspace.Slug}");
    }

    private static bool ValidateInput(string slug) => !string.IsNullOrWhiteSpace(slug);

    private async Task AcceptWorkspaceInviteAsync(UserWorkspace userWorkspace, User user)
    {
        if (!userWorkspace.Accepted)
        {
            userWorkspace.Accepted = true;
            userWorkspace.UpdatedAt = DateTime.UtcNow;
            userWorkspace.UpdatedBy = user.Id;
            await this.userWorkspaceRepository.UpdateAsync(userWorkspace);
        }
    }
}
