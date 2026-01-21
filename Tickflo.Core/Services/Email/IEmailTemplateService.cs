using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Email;

public interface IEmailTemplateService
{
    Task<(string subject, string body)> RenderTemplateAsync(EmailTemplateType templateType, Dictionary<string, string> variables, int? workspaceId = null);
}
