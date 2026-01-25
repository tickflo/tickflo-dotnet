namespace Tickflo.Core.Services.Email;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service implementation for sending emails by creating email records in the database.
/// The actual sending is handled by a CRON job that processes unsent emails.
/// </summary>

/// <summary>
/// Service for sending emails by creating email records in the database.
/// The actual sending is handled by a CRON job that processes unsent emails.
/// </summary>
public interface IEmailSendService
{
    /// <summary>
    /// Sends an email by creating an email record in the database.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="templateType">Email template type to use</param>
    /// <param name="variables">Variables to substitute in the template</param>
    /// <param name="sentByUserId">ID of the user who sent the email</param>
    public Task AddToQueueAsync(string toEmail, EmailTemplateType templateType, Dictionary<string, string> variables, int? sentByUserId = null);
}

public class EmailSendService(
    TickfloDbContext db,
    TickfloConfig config) : IEmailSendService
{
    private readonly TickfloDbContext db = db;
    private readonly TickfloConfig config = config;

    public async Task AddToQueueAsync(
        string toEmail,
        EmailTemplateType templateType,
        Dictionary<string, string>? variables = null,
        int? sentByUserId = null
    )
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Email address is required", nameof(toEmail));
        }

        var template = await this.db.EmailTemplates.Where(t => t.TemplateTypeId == (int)templateType).OrderByDescending(t => t.Version).FirstOrDefaultAsync()
        ?? throw new InvalidOperationException($"No email template found for type {templateType}");

        await this.db.Emails.AddAsync(new Email
        {
            TemplateId = template.Id,
            Vars = variables,
            From = $"{this.config.Email.FromName} <{this.config.Email.FromAddress}>",
            To = toEmail.Trim().ToLowerInvariant(),
            CreatedBy = sentByUserId
        });

        await this.db.SaveChangesAsync();
    }
}
