namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;

[ApiController]
public class EmailConfirmationController(
    IUserRepository users,
    IEmailSenderService emailSenderService,
    IEmailTemplateService emailTemplateService,
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IUserRepository userRepository = users;
    private readonly IEmailSenderService emailSenderService = emailSenderService;
    private readonly IEmailTemplateService emailTemplateService = emailTemplateService;
    private readonly INotificationRepository notificationRepository = notificationRepository;
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
            // If user doesn't have a password yet, redirect to set password
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return this.Redirect($"/set-password?userId={user.Id}");
            }
            return this.Redirect("/email-confirmation/thank-you");
        }

        if (user.EmailConfirmationCode != code)
        {
            return this.BadRequest("Invalid confirmation code.");
        }

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        await this.userRepository.UpdateAsync(user);

        // After confirming email, redirect to set password if user doesn't have one
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return this.Redirect($"/set-password?userId={user.Id}");
        }

        return this.Redirect("/email-confirmation/thank-you");
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
        if (user == null)
        {
            return this.NotFound("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return this.BadRequest("Email is already confirmed.");
        }

        // Generate a new confirmation code
        var newCode = TokenGenerator.GenerateToken(16);
        user.EmailConfirmationCode = newCode;
        await this.userRepository.UpdateAsync(user);

        // Create confirmation link
        var confirmationLink = $"{this.Request.Scheme}://{this.Request.Host}/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(newCode)}";

        var variables = new Dictionary<string, string>
        {
            { "USER_NAME", user.Name },
            { "CONFIRMATION_LINK", confirmationLink }
        };

        var (subject, body) = await this.emailTemplateService.RenderTemplateAsync(EmailTemplateType.EmailConfirmationRequest, variables);

        // Send the email
        await this.emailSenderService.SendAsync(user.Email, subject, body);

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

        await this.notificationRepository.AddAsync(notification);

        return this.Ok(new { message = "Confirmation email resent successfully." });
    }

    [HttpPost("email-confirmation/dismiss")]
    [Authorize]
    public IActionResult Dismiss() =>
        // Dismiss the email confirmation banner for now
        // The user can still be prompted later, but won't see the banner immediately
        this.Ok(new { message = "Email confirmation reminder dismissed." });
}
