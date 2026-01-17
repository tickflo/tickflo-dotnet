using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class EmailTemplateRepository(TickfloDbContext db) : IEmailTemplateRepository
{
    public async Task<EmailTemplate?> FindByTypeAsync(EmailTemplateType templateType, int? workspaceId = null, CancellationToken ct = default)
    {
        // Get the template with the highest version for the given template type
        // workspaceId parameter is kept for interface compatibility but no longer used
        // (templates are now global with versioning)
        return await db.EmailTemplates
            .Where(t => t.TemplateTypeId == (int)templateType)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EmailTemplate?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<List<EmailTemplate>> ListAsync(int? workspaceId = null, CancellationToken ct = default)
    {
        // workspaceId parameter is kept for interface compatibility but no longer used
        // Returns all templates, grouped by template type with only the latest version
        var allTemplates = await db.EmailTemplates
            .OrderByDescending(t => t.Version)
            .ToListAsync(ct);

        // Get only the latest version of each template type
        return allTemplates
            .GroupBy(t => t.TemplateTypeId)
            .Select(g => g.First())
            .OrderBy(t => t.TemplateTypeId)
            .ToList();
    }

    public async Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        // Templates are immutable and versioned
        // If a template with this template_type_id already exists, create a new version
        var latestVersion = await db.EmailTemplates
            .Where(t => t.TemplateTypeId == template.TemplateTypeId)
            .OrderByDescending(t => t.Version)
            .Select(t => t.Version)
            .FirstOrDefaultAsync(ct);

        // Set the version to the next version number
        template.Version = latestVersion + 1;

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    public async Task<EmailTemplate> UpdateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        // Templates are immutable - updates create new versions instead
        // Find the current template to get its template_type_id
        var currentTemplate = await FindByIdAsync(template.Id, ct);
        if (currentTemplate == null)
        {
            throw new InvalidOperationException($"Template with ID {template.Id} not found");
        }

        // Create a new version with the updated content
        var newVersion = new EmailTemplate
        {
            TemplateTypeId = currentTemplate.TemplateTypeId,
            Subject = template.Subject,
            Body = template.Body,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = template.UpdatedBy,
            UpdatedAt = null,
            UpdatedBy = null
        };

        return await CreateAsync(newVersion, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var template = await FindByIdAsync(id, ct);
        if (template != null)
        {
            db.EmailTemplates.Remove(template);
            await db.SaveChangesAsync(ct);
        }
    }
}
