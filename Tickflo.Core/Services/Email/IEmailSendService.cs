namespace Tickflo.Core.Services.Email;

using Tickflo.Core.Entities;

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
    /// <param name="workspaceId">Optional workspace ID for workspace-specific templates</param>
    /// <returns>The created email record</returns>
    Task<Email> SendAsync(
        string toEmail,
        EmailTemplateType templateType,
        Dictionary<string, string> variables,
        int? workspaceId = null);
}
