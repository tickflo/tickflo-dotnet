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
    IWorkspaceRepository workspaces,
    IUserWorkspaceRepository userWorkspaceRepo,
    IInventoryRepository inventoryRepo,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceInventoryViewService viewService) : WorkspacePageModel
{
    #region Constants
    private const string InventorySection = "inventory";
    private const string EditAction = "edit";
    private const string ArchivedStatus = "archived";
    private const string ActiveStatus = "active";
    private const string ItemArchivedMessage = "Inventory item archived.";
    private const string ItemRestoredMessage = "Inventory item restored.";
    #endregion

    private readonly IWorkspaceRepository _workspaces = workspaces;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IInventoryRepository _inventoryRepo = inventoryRepo;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly IWorkspaceInventoryViewService _viewService = viewService;

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

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this._workspaces, this._userWorkspaceRepo, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, uid);
        this.IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        this.CanCreateInventory = viewData.CanCreateInventory;
        this.CanEditInventory = viewData.CanEditInventory;

        this.Items = viewData.Items;
        if (!string.IsNullOrWhiteSpace(this.Query))
        {
            this.Items = this.Items.Where(i => i.Name?.Contains(this.Query, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        }
        if (!string.IsNullOrWhiteSpace(this.Status))
        {
            this.Items = this.Items.Where(i => i.Status == this.Status).ToList();
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

        var item = await this._inventoryRepo.FindAsync(this.Workspace!.Id, id);
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

        var item = await this._inventoryRepo.FindAsync(this.Workspace!.Id, id);
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
        this.Workspace = await this._workspaces.FindBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this._currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Forbid();
        }

        var allowed = await this._workspaceAccessService.CanUserPerformActionAsync(
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
        await this._inventoryRepo.UpdateAsync(item);
    }

    private IActionResult RedirectToInventoryPage() => this.Redirect($"/workspaces/{this.Workspace!.Slug}/inventory");
}

