using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _templateRepo;

    public EmailTemplateService(IEmailTemplateRepository templateRepo)
    {
        _templateRepo = templateRepo;
    }

    public async Task<(string subject, string body)> RenderTemplateAsync(
        int templateTypeId, 
        Dictionary<string, string> variables, 
        int? workspaceId = null)
    {
        var template = await _templateRepo.FindByTypeAsync(templateTypeId, workspaceId);
        
        if (template == null)
        {
            throw new InvalidOperationException($"Email template with type ID {templateTypeId} not found.");
        }

        var subject = template.Subject;
        var body = template.Body;

        // Replace all variables in subject and body
        foreach (var kvp in variables)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            subject = subject.Replace(placeholder, kvp.Value);
            body = body.Replace(placeholder, kvp.Value);
        }

        return (subject, body);
    }
}
