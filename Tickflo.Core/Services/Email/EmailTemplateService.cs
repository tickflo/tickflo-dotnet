namespace Tickflo.Core.Services.Email;

using System.Text;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class EmailTemplateService(IEmailTemplateRepository emailTemplateRepository) : IEmailTemplateService
{
    #region Constants
    private static readonly CompositeFormat TemplateNotFoundErrorFormat = CompositeFormat.Parse("Email template with type ID {0} not found.");
    #endregion

    private readonly IEmailTemplateRepository emailTemplateRepository = emailTemplateRepository;

    public async Task<(string subject, string body)> RenderTemplateAsync(
        EmailTemplateType templateType,
        Dictionary<string, string> variables,
        int? workspaceId = null)
    {
        var template = await this.GetTemplateOrThrowAsync(templateType, workspaceId);

        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return (subject, body);
    }

    private async Task<EmailTemplate> GetTemplateOrThrowAsync(EmailTemplateType templateType, int? workspaceId)
    {
        var template = await this.emailTemplateRepository.FindByTypeAsync(templateType, workspaceId) ?? throw new InvalidOperationException(string.Format(null, TemplateNotFoundErrorFormat, (int)templateType));

        return template;
    }

    private static string ReplaceVariables(string text, Dictionary<string, string> variables)
    {
        var result = text;
        foreach (var kvp in variables)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            result = result.Replace(placeholder, kvp.Value);
        }
        return result;
    }
}
