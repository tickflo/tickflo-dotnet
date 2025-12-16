using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;

namespace Tickflo.Web.Controllers;

[ApiController]
public class EmailConfirmationController : ControllerBase
{
    private readonly IUserRepository _users;

    public EmailConfirmationController(IUserRepository users)
    {
        _users = users;
    }

    [HttpGet("email-confirmation/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm([FromQuery] string email, [FromQuery] string code)
    {
        var user = await _users.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user == null)
            return NotFound();
        if (user.EmailConfirmationCode != code)
            return BadRequest("Invalid confirmation code.");
        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        await _users.UpdateAsync(user);
        return Redirect("/login");
    }

    [HttpPost("email-confirmation/resend")]
    public IActionResult Resend()
    {
        // In a real implementation, generate a code and send email again.
        // For now, just set a temp message.
        return Ok(new { message = "Resent (stub)." });
    }

    [HttpPost("email-confirmation/dismiss")]
    public IActionResult Dismiss()
    {
        return Ok(new { message = "Dismissed." });
    }
}
