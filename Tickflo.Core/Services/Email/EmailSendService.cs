namespace Tickflo.Core.Services.Email;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service implementation for sending emails by creating email records in the database.
/// The actual sending is handled by a CRON job that processes unsent emails.
/// </summary>
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
    }
}
