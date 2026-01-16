using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Controllers;

[ApiController]
public class WorkspaceInviteController : ControllerBase
{
    #region Constants
    private const string InvalidTokenError = "Invalid or expired token.";
    #endregion

    private readonly IWorkspaceRepository _workspaces;
    private readonly ITokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRepository _userWorkspaces;

    public WorkspaceInviteController(IWorkspaceRepository workspaces, ITokenRepository tokens, IUserRepository users, IUserWorkspaceRepository userWorkspaces)
    {
        _workspaces = workspaces;
        _tokens = tokens;
        _users = users;
        _userWorkspaces = userWorkspaces;
    }

    [HttpGet("workspaces/{slug}/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(string slug, [FromQuery] string token)
    {
        if (!ValidateInput(slug, token))
            return BadRequest(InvalidTokenError);

        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();

        var tok = await _tokens.FindByValueAsync(token);
        if (tok == null) return BadRequest(InvalidTokenError);

        var user = await _users.FindByIdAsync(tok.UserId);
        if (user == null) return NotFound();

        var uw = await _userWorkspaces.FindAsync(user.Id, ws.Id);
        if (uw == null) return NotFound();

        await AcceptWorkspaceInviteAsync(uw, user);
        await ConfirmUserEmailIfNeededAsync(user);

        var reset = await _tokens.CreatePasswordResetForUserIdAsync(user.Id);
        return Redirect($"/account/set-password?token={Uri.EscapeDataString(reset.Value)}");
    }

    private bool ValidateInput(string slug, string token)
    {
        return !string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(token);
    }

    private async Task AcceptWorkspaceInviteAsync(UserWorkspace userWorkspace, User user)
    {
        if (!userWorkspace.Accepted)
        {
            userWorkspace.Accepted = true;
            userWorkspace.UpdatedAt = DateTime.UtcNow;
            userWorkspace.UpdatedBy = user.Id;
            await _userWorkspaces.UpdateAsync(userWorkspace);
        }
    }

    private async Task ConfirmUserEmailIfNeededAsync(User user)
    {
        if (!user.EmailConfirmed && !string.IsNullOrEmpty(user.EmailConfirmationCode))
        {
            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null;
            await _users.UpdateAsync(user);
        }
    }
}
