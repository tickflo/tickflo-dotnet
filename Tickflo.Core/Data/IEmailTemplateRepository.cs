namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IEmailTemplateRepository
{
    public Task<EmailTemplate?> FindByTypeAsync(EmailTemplateType templateType, int? workspaceId = null, CancellationToken ct = default);
    public Task<EmailTemplate?> FindByIdAsync(int id, CancellationToken ct = default);
    public Task<List<EmailTemplate>> ListAsync(int? workspaceId = null, CancellationToken ct = default);
    public Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default);
    public Task<EmailTemplate> UpdateAsync(EmailTemplate template, CancellationToken ct = default);
    public Task DeleteAsync(int id, CancellationToken ct = default);
}
