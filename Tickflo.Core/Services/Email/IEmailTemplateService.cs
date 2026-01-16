namespace Tickflo.Core.Services.Email;

public interface IEmailTemplateService
{
    Task<(string subject, string body)> RenderTemplateAsync(int templateTypeId, Dictionary<string, string> variables, int? workspaceId = null);
}
