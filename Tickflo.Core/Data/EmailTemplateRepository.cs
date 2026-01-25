namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public interface IEmailTemplateRepository
{
    public Task<EmailTemplate?> FindByTypeAsync(EmailTemplateType templateType, int? workspaceId = null, CancellationToken ct = default);
    public Task<EmailTemplate?> FindByIdAsync(int id, CancellationToken ct = default);
    public Task<List<EmailTemplate>> ListAsync(int? workspaceId = null, CancellationToken ct = default);
    public Task<EmailTemplate> CreateAsync(EmailTemplate emailTemplate, CancellationToken ct = default);
    public Task<EmailTemplate> UpdateAsync(EmailTemplate emailTemplate, CancellationToken ct = default);
    public Task DeleteAsync(int id, CancellationToken ct = default);
}


public class EmailTemplateRepository(TickfloDbContext dbContext) : IEmailTemplateRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<EmailTemplate?> FindByTypeAsync(EmailTemplateType templateType, int? workspaceId = null, CancellationToken ct = default) =>
        // Get the template with the highest version for the given template type
        // workspaceId parameter is kept for interface compatibility but no longer used
        // (templates are now global with versioning)
        await this.dbContext.EmailTemplates
            .Where(t => t.TemplateTypeId == (int)templateType)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(ct);

    public async Task<EmailTemplate?> FindByIdAsync(int id, CancellationToken ct = default) => await this.dbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<EmailTemplate>> ListAsync(int? workspaceId = null, CancellationToken ct = default)
    {
        // workspaceId parameter is kept for interface compatibility but no longer used
        // Returns all templates, grouped by template type with only the latest version
        var allTemplates = await this.dbContext.EmailTemplates
            .OrderByDescending(t => t.Version)
            .ToListAsync(ct);

        // Get only the latest version of each template type
        return [.. allTemplates
            .GroupBy(t => t.TemplateTypeId)
            .Select(g => g.First())
            .OrderBy(t => t.TemplateTypeId)];
    }

    public async Task<EmailTemplate> CreateAsync(EmailTemplate emailTemplate, CancellationToken ct = default)
    {
        // Templates are immutable and versioned
        // If a template with this template_type_id already exists, create a new version
        var latestVersion = await this.dbContext.EmailTemplates
            .Where(t => t.TemplateTypeId == emailTemplate.TemplateTypeId)
            .OrderByDescending(t => t.Version)
            .Select(t => t.Version)
            .FirstOrDefaultAsync(ct);

        // Set the version to the next version number
        emailTemplate.Version = latestVersion + 1;

        this.dbContext.EmailTemplates.Add(emailTemplate);
        await this.dbContext.SaveChangesAsync(ct);
        return emailTemplate;
    }

    public async Task<EmailTemplate> UpdateAsync(EmailTemplate emailTemplate, CancellationToken ct = default)
    {
        // Templates are immutable - updates create new versions instead
        // Find the current template to get its template_type_id
        var currentTemplate = await this.FindByIdAsync(emailTemplate.Id, ct) ?? throw new InvalidOperationException($"Template with ID {emailTemplate.Id} not found");

        // Create a new version with the updated content
        var newVersion = new EmailTemplate
        {
            TemplateTypeId = currentTemplate.TemplateTypeId,
            Subject = emailTemplate.Subject,
            Body = emailTemplate.Body,
            CreatedBy = emailTemplate.UpdatedBy,
            UpdatedAt = null,
            UpdatedBy = null
        };

        return await this.CreateAsync(newVersion, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var template = await this.FindByIdAsync(id, ct);
        if (template != null)
        {
            this.dbContext.EmailTemplates.Remove(template);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }
}
