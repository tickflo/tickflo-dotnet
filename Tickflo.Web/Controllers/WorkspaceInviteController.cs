namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

[ApiController]
public class WorkspaceInviteController(IWorkspaceRepository workspaces, ITokenRepository tokens, IUserRepository users, IUserWorkspaceRepository userWorkspaces) : ControllerBase
{
    #region Constants
    private const string InvalidTokenError = "Invalid or expired token.";
    #endregion

    private readonly IWorkspaceRepository _workspaces = workspaces;
    private readonly ITokenRepository _tokens = tokens;
    private readonly IUserRepository _users = users;
    private readonly IUserWorkspaceRepository _userWorkspaces = userWorkspaces;

    [HttpGet("workspaces/{slug}/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(string slug, [FromQuery] string token)
    {
        if (!ValidateInput(slug, token))
        {
            return this.BadRequest(InvalidTokenError);
        }

        var ws = await this._workspaces.FindBySlugAsync(slug);
        if (ws == null)
        {
            return this.NotFound();
        }

        var tok = await this._tokens.FindByValueAsync(token);
        if (tok == null)
        {
            return this.BadRequest(InvalidTokenError);
        }

        var user = await this._users.FindByIdAsync(tok.UserId);
        if (user == null)
        {
            return this.NotFound();
        }

        var uw = await this._userWorkspaces.FindAsync(user.Id, ws.Id);
        if (uw == null)
        {
            return this.NotFound();
        }

        await this.AcceptWorkspaceInviteAsync(uw, user);
        await this.ConfirmUserEmailIfNeededAsync(user);

        var reset = await this._tokens.CreatePasswordResetForUserIdAsync(user.Id);
        return this.Redirect($"/account/set-password?token={Uri.EscapeDataString(reset.Value)}");
    }

    private static bool ValidateInput(string slug, string token) => !string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(token);

    private async Task AcceptWorkspaceInviteAsync(UserWorkspace userWorkspace, User user)
    {
        if (!userWorkspace.Accepted)
        {
            userWorkspace.Accepted = true;
            userWorkspace.UpdatedAt = DateTime.UtcNow;
            userWorkspace.UpdatedBy = user.Id;
            await this._userWorkspaces.UpdateAsync(userWorkspace);
        }
    }

    private async Task ConfirmUserEmailIfNeededAsync(User user)
    {
        if (!user.EmailConfirmed && !string.IsNullOrEmpty(user.EmailConfirmationCode))
        {
            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null;
            await this._users.UpdateAsync(user);
        }
    }
}
