namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services.Users;

[ApiController]
public class WorkspaceInviteController(
    IUserInvitationService userInvitationService,
    ILogger<WorkspaceInviteController> logger
    ) : ControllerBase
{
    private readonly IUserInvitationService userInvitationService = userInvitationService;
    private readonly ILogger<WorkspaceInviteController> logger = logger;

    [HttpPost("workspaces/{slug}/accept")]
    [Authorize]
    public async Task<IActionResult> Accept([FromServices] IAppContext appContext, string slug)
    {
        var user = appContext.CurrentUser;
        if (user == null)
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return this.Redirect("/workspaces");
        }

        try
        {
            await this.userInvitationService.AcceptInvitationAsync(slug, user.Id);
            return this.Redirect($"/workspaces/{slug}");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error accepting invitation for user {UserId} to workspace {Slug}", user.Id, slug);
            return this.Redirect("/workspaces");
        }
    }

    [HttpPost("workspaces/{slug}/decline")]
    [Authorize]
    public async Task<IActionResult> Decline([FromServices] IAppContext appContext, string slug)
    {
        var user = appContext.CurrentUser;
        if (user == null)
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return this.Redirect("/workspaces");
        }

        try
        {
            await this.userInvitationService.DeclineInvitationAsync(slug, user.Id);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error declining invitation for user {UserId} to workspace {Slug}", user.Id, slug);
        }

        return this.Redirect("/workspaces");
    }
}
