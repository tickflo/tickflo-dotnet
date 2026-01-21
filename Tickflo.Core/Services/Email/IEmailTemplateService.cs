namespace Tickflo.Core.Services.Email;

using Tickflo.Core.Entities;

public interface IEmailTemplateService
{
    public Task<(string subject, string body)> RenderTemplateAsync(EmailTemplateType templateType, Dictionary<string, string> variables, int? workspaceId = null);
}
