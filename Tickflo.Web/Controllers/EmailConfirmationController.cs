using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;

namespace Tickflo.Web.Controllers;

[ApiController]
public class EmailConfirmationController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IEmailSender _emailSender;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public EmailConfirmationController(
        IUserRepository users,
        IEmailSender emailSender,
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _emailSender = emailSender;
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    [HttpGet("email-confirmation/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm([FromQuery] string email, [FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Invalid confirmation request.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _users.FindByEmailAsync(normalizedEmail);
        if (user == null)
        {
            return NotFound();
        }

        if (user.EmailConfirmed)
        {
            return Redirect("/email-confirmation/thank-you");
        }

        if (user.EmailConfirmationCode != code)
        {
            return BadRequest("Invalid confirmation code.");
        }

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        await _users.UpdateAsync(user);
        return Redirect("/email-confirmation/thank-you");
    }

    [HttpPost("email-confirmation/resend")]
    [Authorize]
    public async Task<IActionResult> Resend()
    {
        // Get the current authenticated user
        if (!_currentUserService.TryGetUserId(User, out var userId))
        {
            return Unauthorized();
        }

        var user = await _users.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return BadRequest("Email is already confirmed.");
        }

        // Generate a new confirmation code
        var newCode = TokenGenerator.GenerateToken(16);
        user.EmailConfirmationCode = newCode;
        await _users.UpdateAsync(user);

        // Create confirmation link
        var confirmationLink = $"{Request.Scheme}://{Request.Host}/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(newCode)}";

        // Prepare email content
        var subject = "Confirm Your Email Address";
        var body = $@"<div style='font-family:Arial,sans-serif'>
                        <h2 style='color:#333'>Email Confirmation</h2>
                        <p>Hello {user.Name},</p>
                        <p>Please confirm your email address by clicking the link below:</p>
                        <p><a href=""{confirmationLink}"" style='background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block'>Confirm Email</a></p>
                        <p>Or copy and paste this link: <br/>{confirmationLink}</p>
                        <hr/>
                        <p style='color:#777;font-size:12px'>If you did not request this email, you can ignore it.</p>
                     </div>";

        // Send the email
        await _emailSender.SendAsync(user.Email, subject, body);

        // Create a notification record in the database
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = null,
            Type = "email_confirmation",
            DeliveryMethod = "email",
            Priority = "high",
            Subject = subject,
            Body = body,
            Status = "sent",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _notificationRepository.AddAsync(notification);

        return Ok(new { message = "Confirmation email resent successfully." });
    }

    [HttpPost("email-confirmation/dismiss")]
    [Authorize]
    public IActionResult Dismiss()
    {
        // Dismiss the email confirmation banner for now
        // The user can still be prompted later, but won't see the banner immediately
        return Ok(new { message = "Email confirmation reminder dismissed." });
    }
}
