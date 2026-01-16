using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class EmailTemplateRepository(TickfloDbContext db) : IEmailTemplateRepository
{
    public async Task<EmailTemplate?> FindByTypeAsync(int templateTypeId, int? workspaceId = null, CancellationToken ct = default)
    {
        // First try to find workspace-specific template, then fall back to global (null workspace_id)
        var query = db.EmailTemplates
            .Where(t => t.TemplateTypeId == templateTypeId);

        if (workspaceId.HasValue)
        {
            var workspaceTemplate = await query
                .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId, ct);
            
            if (workspaceTemplate != null)
                return workspaceTemplate;
        }

        // Fall back to global template
        return await query
            .FirstOrDefaultAsync(t => t.WorkspaceId == null, ct);
    }

    public async Task<EmailTemplate?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<List<EmailTemplate>> ListAsync(int? workspaceId = null, CancellationToken ct = default)
    {
        var query = db.EmailTemplates.AsQueryable();

        if (workspaceId.HasValue)
        {
            query = query.Where(t => t.WorkspaceId == workspaceId || t.WorkspaceId == null);
        }
        else
        {
            query = query.Where(t => t.WorkspaceId == null);
        }

        return await query
            .OrderBy(t => t.TemplateTypeId)
            .ToListAsync(ct);
    }

    public async Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    public async Task<EmailTemplate> UpdateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        db.EmailTemplates.Update(template);
        await db.SaveChangesAsync(ct);
        return template;
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
