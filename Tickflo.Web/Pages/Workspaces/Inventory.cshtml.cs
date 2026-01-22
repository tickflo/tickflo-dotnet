namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class InventoryModel(
    IWorkspaceService workspaceService,
    IInventoryRepository inventoryRepository,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceInventoryViewService workspaceInventoryViewService) : WorkspacePageModel
{
    #region Constants
    private const string InventorySection = "inventory";
    private const string EditAction = "edit";
    private const string ArchivedStatus = "archived";
    private const string ActiveStatus = "active";
    private const string ItemArchivedMessage = "Inventory item archived.";
    private const string ItemRestoredMessage = "Inventory item restored.";
    #endregion

    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IInventoryRepository inventoryRepository = inventoryRepository;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IWorkspaceInventoryViewService workspaceInventoryViewService = workspaceInventoryViewService;

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public bool IsWorkspaceAdmin { get; private set; }
    public bool CanCreateInventory { get; private set; }
    public bool CanEditInventory { get; private set; }
    public Workspace? Workspace { get; set; }
    public IEnumerable<Inventory> Items { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
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

        var viewData = await this.workspaceInventoryViewService.BuildAsync(this.Workspace!.Id, uid);
        this.IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        this.CanCreateInventory = viewData.CanCreateInventory;
        this.CanEditInventory = viewData.CanEditInventory;

        this.Items = viewData.Items;
        if (!string.IsNullOrWhiteSpace(this.Query))
        {
            this.Items = [.. this.Items.Where(i => i.Name?.Contains(this.Query, StringComparison.OrdinalIgnoreCase) ?? false)];
        }
        if (!string.IsNullOrWhiteSpace(this.Status))
        {
            this.Items = [.. this.Items.Where(i => i.Status == this.Status)];
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostArchiveAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeInventoryEditAsync(slug) is IActionResult authResult)
        {
            return authResult;
        }

        var item = await this.inventoryRepository.FindAsync(this.Workspace!.Id, id);
        if (item == null)
        {
            return this.NotFound();
        }

        await this.UpdateInventoryStatusAsync(item, ArchivedStatus);
        this.SetSuccessMessage(ItemArchivedMessage);

        return this.RedirectToInventoryPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(string slug, int id)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeInventoryEditAsync(slug) is IActionResult authResult)
        {
            return authResult;
        }

        var item = await this.inventoryRepository.FindAsync(this.Workspace!.Id, id);
        if (item == null)
        {
            return this.NotFound();
        }

        await this.UpdateInventoryStatusAsync(item, ActiveStatus);
        this.SetSuccessMessage(ItemRestoredMessage);

        return this.RedirectToInventoryPage();
    }

    private async Task<IActionResult?> AuthorizeInventoryEditAsync(string slug)
    {
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Forbid();
        }

        var allowed = await this.workspaceAccessService.CanUserPerformActionAsync(
            this.Workspace.Id, uid, InventorySection, EditAction);

        if (!allowed)
        {
            return this.Forbid();
        }

        return null;
    }

    private async Task UpdateInventoryStatusAsync(Inventory item, string status)
    {
        item.Status = status;
        await this.inventoryRepository.UpdateAsync(item);
    }

    private RedirectResult RedirectToInventoryPage() => this.Redirect($"/workspaces/{this.Workspace!.Slug}/inventory");
}

