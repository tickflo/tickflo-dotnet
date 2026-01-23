namespace Tickflo.Core.Services.Email;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service implementation for sending emails by creating email records in the database.
/// The actual sending is handled by a CRON job that processes unsent emails.
/// </summary>
public class EmailSendService(
    IEmailRepository emailRepository,
    IEmailTemplateRepository emailTemplateRepository) : IEmailSendService
{
    private readonly IEmailRepository emailRepository = emailRepository;
    private readonly IEmailTemplateRepository emailTemplateRepository = emailTemplateRepository;

    public async Task<Email> SendAsync(
        string toEmail,
        EmailTemplateType templateType,
        Dictionary<string, string> variables,
        int? workspaceId = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Email address is required", nameof(toEmail));
        }

        // Get the template ID from the template type
        var template = await this.emailTemplateRepository.FindByTypeAsync(templateType, workspaceId);
        if (template == null)
        {
            throw new InvalidOperationException($"Email template not found for type {templateType}");
        }

        // Create email record
        var email = new Email
        {
            TemplateId = template.Id,
            Vars = variables ?? [],
            From = "noreply@tickflo.co",
            To = toEmail.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        return await this.emailRepository.AddAsync(email);
    }
}
