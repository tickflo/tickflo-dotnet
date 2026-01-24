namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Common;

[ApiController]
public class EmailConfirmationController(
    IUserRepository users,
    ICurrentUserService currentUserService,
    IAuthenticationService authenticationService) : ControllerBase
{
    private readonly IUserRepository userRepository = users;
    private readonly IAuthenticationService authenticationService = authenticationService;
    private readonly ICurrentUserService currentUserService = currentUserService;

    [HttpGet("email-confirmation/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm([FromQuery] string email, [FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return this.BadRequest("Invalid confirmation request.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await this.userRepository.FindByEmailAsync(normalizedEmail);
        if (user == null)
        {
            return this.NotFound();
        }

        if (user.EmailConfirmed)
        {
            return this.Redirect("/workspaces");
        }

        if (user.EmailConfirmationCode != code)
        {
            return this.BadRequest("Invalid confirmation code.");
        }

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        await this.userRepository.UpdateAsync(user);

        return this.Redirect("/workspaces");
    }

    [HttpPost("email-confirmation/resend")]
    [Authorize]
    public async Task<IActionResult> Resend()
    {
        // Get the current authenticated user
        if (!this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            return this.Unauthorized();
        }

        var user = await this.userRepository.FindByIdAsync(userId);
        if (user == null || user.EmailConfirmed)
        {
            return this.Redirect("/workspaces");
        }

        try
        {
            await this.authenticationService.ResendEmailConfirmationAsync(user.Id);
            return this.Ok(new { message = "Confirmation email resent successfully." });
        }
        catch (Exception ex)
        {
            return this.StatusCode(500, new { message = "Failed to resend confirmation email.", detail = ex.Message });
        }

    }

    [HttpPost("email-confirmation/dismiss")]
    [Authorize]
    public IActionResult Dismiss() =>
        // Dismiss the email confirmation banner for now
        // The user can still be prompted later, but won't see the banner immediately
        this.Ok(new { message = "Email confirmation reminder dismissed." });
}
