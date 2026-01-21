using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    #region Constants
    private const string TemplateNotFoundErrorFormat = "Email template with type ID {0} not found.";
    #endregion

    private readonly IEmailTemplateRepository _templateRepo;

    public EmailTemplateService(IEmailTemplateRepository templateRepo)
    {
        _templateRepo = templateRepo;
    }

    public async Task<(string subject, string body)> RenderTemplateAsync(
        EmailTemplateType templateType, 
        Dictionary<string, string> variables, 
        int? workspaceId = null)
    {
        var template = await GetTemplateOrThrowAsync(templateType, workspaceId);
        
        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return (subject, body);
    }

    private async Task<EmailTemplate> GetTemplateOrThrowAsync(EmailTemplateType templateType, int? workspaceId)
    {
        var template = await _templateRepo.FindByTypeAsync(templateType, workspaceId);
        
        if (template == null)
        {
            throw new InvalidOperationException(string.Format(TemplateNotFoundErrorFormat, (int)templateType));
        }
        
        return template;
    }

    private string ReplaceVariables(string text, Dictionary<string, string> variables)
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
