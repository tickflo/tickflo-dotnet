using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;

namespace Tickflo.Web.Controllers;

[ApiController]
public class WorkspaceInviteController : ControllerBase
{
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
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Invalid or expired token.");
        }

        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();

        var tok = await _tokens.FindByValueAsync(token);
        if (tok == null) return BadRequest("Invalid or expired token.");

        var user = await _users.FindByIdAsync(tok.UserId);
        if (user == null) return NotFound();

        var uw = await _userWorkspaces.FindAsync(user.Id, ws.Id);
        if (uw == null) return NotFound();

        if (!uw.Accepted)
        {
            uw.Accepted = true;
            uw.UpdatedAt = DateTime.UtcNow;
            uw.UpdatedBy = user.Id;
            await _userWorkspaces.UpdateAsync(uw);
        }

        if (!user.EmailConfirmed && !string.IsNullOrEmpty(user.EmailConfirmationCode))
        {
            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null;
            await _users.UpdateAsync(user);
        }

        var reset = await _tokens.CreatePasswordResetForUserIdAsync(user.Id);
        return Redirect($"/account/set-password?token={Uri.EscapeDataString(reset.Value)}");
    }
}
