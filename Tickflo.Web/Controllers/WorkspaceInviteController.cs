namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

[ApiController]
public class WorkspaceInviteController(IWorkspaceRepository workspaceRepository, ITokenRepository tokenRepository, IUserRepository users, IUserWorkspaceRepository userWorkspaceRepository) : ControllerBase
{
    #region Constants
    private const string InvalidTokenError = "Invalid or expired token.";
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly ITokenRepository tokenRepository = tokenRepository;
    private readonly IUserRepository userRepository = users;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;

    [HttpGet("workspaces/{slug}/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(string slug, [FromQuery] string token)
    {
        if (!ValidateInput(slug, token))
        {
            return this.BadRequest(InvalidTokenError);
        }

        var workspace = await this.workspaceRepository.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        var tok = await this.tokenRepository.FindByValueAsync(token);
        if (tok == null)
        {
            return this.BadRequest(InvalidTokenError);
        }

        var user = await this.userRepository.FindByIdAsync(tok.UserId);
        if (user == null)
        {
            return this.NotFound();
        }

        var uw = await this.userWorkspaceRepository.FindAsync(user.Id, workspace.Id);
        if (uw == null)
        {
            return this.NotFound();
        }

        await this.AcceptWorkspaceInviteAsync(uw, user);
        await this.ConfirmUserEmailIfNeededAsync(user);

        var reset = await this.tokenRepository.CreatePasswordResetForUserIdAsync(user.Id);
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
            await this.userWorkspaceRepository.UpdateAsync(userWorkspace);
        }
    }

    private async Task ConfirmUserEmailIfNeededAsync(User user)
    {
        if (!user.EmailConfirmed && !string.IsNullOrEmpty(user.EmailConfirmationCode))
        {
            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null;
            await this.userRepository.UpdateAsync(user);
        }
    }
}
