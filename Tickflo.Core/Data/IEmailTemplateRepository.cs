using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> FindByTypeAsync(int templateTypeId, int? workspaceId = null, CancellationToken ct = default);
    Task<EmailTemplate?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<List<EmailTemplate>> ListAsync(int? workspaceId = null, CancellationToken ct = default);
    Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default);
    Task<EmailTemplate> UpdateAsync(EmailTemplate template, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
