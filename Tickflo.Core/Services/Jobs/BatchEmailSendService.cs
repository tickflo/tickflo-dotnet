namespace Tickflo.Core.Jobs;

using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tickflo.Core.Config;
using Tickflo.Core.Data;

public interface IBatchEmailSendService
{
    /// <summary>
    /// Processes unsent emails in batches and sends them.
    /// </summary>
    public Task ProcessEmailQueueAsync();
}

public class MailgunEmailSendService(
    TickfloDbContext db,
    TickfloConfig config,
    ILogger<MailgunEmailSendService> logger) : IBatchEmailSendService
{
    private readonly TickfloDbContext db = db;
    private readonly TickfloConfig config = config;
    private readonly ILogger<MailgunEmailSendService> logger = logger;

    public async Task ProcessEmailQueueAsync()
    {
        if (string.IsNullOrEmpty(this.config.MailgunApiKey))
        {
            this.logger.LogError("Mailgun API key is not configured. Email queue is blocked — no emails will be sent until configured.");
            return;
        }

        var unsentEmails = await this.db.Emails.Where(e => e.SentAt == null).OrderBy(e => e.CreatedAt).Take(this.config.Email.BatchSize).ToListAsync();
        if (unsentEmails.Count == 0)
        {
            return;
        }

        var emailTemplateIds = unsentEmails.Select(e => e.TemplateId).Distinct().ToList();

        var emailTemplates = await this.db.EmailTemplates
            .Where(t => emailTemplateIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        if (emailTemplates.Count != emailTemplateIds.Count)
        {
            var missingTemplateIds = emailTemplateIds.Except(emailTemplates.Keys);
            this.logger.LogError("Missing email templates for IDs: {MissingTemplateIds}", string.Join(", ", missingTemplateIds));
            return;
        }

        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(this.config.Email.MailgunApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"api:{this.config.MailgunApiKey}")))
            },
        };

        foreach (var email in unsentEmails)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "from", $"{this.config.Email.FromName} <{this.config.Email.FromAddress}>" },
                    { "to", email.To },
                    { "subject", RenderTemplate(emailTemplates[email.TemplateId].Subject, email.Vars) },
                    { "html", RenderTemplate(emailTemplates[email.TemplateId].Body, email.Vars).Replace("\n", "<br>") }
                };

                if (this.config.AppEnv != "Production")
                {
                    formData.Add("o:testmode", "true");
                }

                var response = await httpClient.PostAsync($"v3/{this.config.Email.MailgunDomain}/messages", new FormUrlEncodedContent(formData));
                if (response.IsSuccessStatusCode)
                {
                    email.SentAt = DateTime.UtcNow;
                    email.State = "sent";
                    this.logger.LogInformation("Successfully sent email with ID {EmailId}", email.Id);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    this.logger.LogError("Failed to send email with ID {EmailId}. Status: {StatusCode}, Response: {Response}", email.Id, response.StatusCode, errorContent);
                    email.State = "failed";
                    email.ErrorMessage = $"Status: {response.StatusCode}, Response: {errorContent}";
                }

                await this.db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to send email with ID {EmailId}", email.Id);
                email.State = "failed";
                email.ErrorMessage = ex.Message;
                await this.db.SaveChangesAsync();
            }
        }
    }

    private static string RenderTemplate(string template, Dictionary<string, string>? vars)
    {
        if (vars == null)
        {
            return template;
        }

        var result = template;
        foreach (var (key, value) in vars)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }

        return result;
    }
}
