namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class InboundEmailRoutesModel(
    TickfloDbContext dbContext,
    IWorkspaceService workspaceService,
    TickfloConfig config) : WorkspacePageModel
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly TickfloConfig config = config;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<InboundEmailRoute> Routes { get; private set; } = [];
    public int? EditRouteId { get; set; }
    public string InboundDomain => this.config.InboundEmail.Domain;

    [BindProperty]
    public string LocalPart { get; set; } = string.Empty;

    [BindProperty]
    public string Label { get; set; } = string.Empty;

    [BindProperty]
    public string? DefaultTicketType { get; set; }

    [BindProperty]
    public string? DefaultTicketPriority { get; set; }

    [BindProperty]
    public int? DefaultLocationId { get; set; }

    [BindProperty]
    public bool Active { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(string slug, int? edit)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        await this.LoadRoutesAsync(this.Workspace.Id);

        if (edit.HasValue)
        {
            var route = this.Routes.FirstOrDefault(r => r.Id == edit.Value);
            if (route != null)
            {
                this.EditRouteId = route.Id;
                this.LocalPart = route.LocalPart;
                this.Label = route.Label;
                this.DefaultTicketType = route.DefaultTicketType;
                this.DefaultTicketPriority = route.DefaultTicketPriority;
                this.DefaultLocationId = route.DefaultLocationId;
                this.Active = route.Active;
            }
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        if (string.IsNullOrWhiteSpace(this.LocalPart))
        {
            return this.RedirectToPage();
        }

        var existing = await this.dbContext.InboundEmailRoutes
            .AnyAsync(r => r.WorkspaceId == this.Workspace.Id && r.LocalPart == this.LocalPart.Trim().ToLowerInvariant());

        if (existing)
        {
            return this.RedirectToPage();
        }

        var route = new InboundEmailRoute
        {
            WorkspaceId = this.Workspace.Id,
            LocalPart = this.LocalPart.Trim().ToLowerInvariant(),
            Label = this.Label.Trim(),
            DefaultTicketType = this.DefaultTicketType,
            DefaultTicketPriority = this.DefaultTicketPriority,
            DefaultLocationId = this.DefaultLocationId,
            Active = this.Active,
            CreatedByUserId = uid,
            CreatedAt = DateTime.UtcNow,
        };

        this.dbContext.InboundEmailRoutes.Add(route);
        await this.dbContext.SaveChangesAsync();

        return this.RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostUpdateAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var route = await this.dbContext.InboundEmailRoutes
            .FirstOrDefaultAsync(r => r.Id == id && r.WorkspaceId == this.Workspace.Id);

        if (route == null)
        {
            return this.NotFound();
        }

        route.LocalPart = this.LocalPart.Trim().ToLowerInvariant();
        route.Label = this.Label.Trim();
        route.DefaultTicketType = this.DefaultTicketType;
        route.DefaultTicketPriority = this.DefaultTicketPriority;
        route.DefaultLocationId = this.DefaultLocationId;
        route.Active = this.Active;
        route.UpdatedByUserId = uid;
        route.UpdatedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync();

        return this.RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var route = await this.dbContext.InboundEmailRoutes
            .FirstOrDefaultAsync(r => r.Id == id && r.WorkspaceId == this.Workspace.Id);

        if (route != null)
        {
            this.dbContext.InboundEmailRoutes.Remove(route);
            await this.dbContext.SaveChangesAsync();
        }

        return this.RedirectToPage(new { slug });
    }

    private async Task LoadRoutesAsync(int workspaceId) => this.Routes = await this.dbContext.InboundEmailRoutes
            .Where(r => r.WorkspaceId == workspaceId)
            .OrderBy(r => r.LocalPart)
            .ToListAsync();
}
