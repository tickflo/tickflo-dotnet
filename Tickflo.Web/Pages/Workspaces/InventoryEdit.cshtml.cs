namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Services.Views;

[Authorize]
public class InventoryEditModel(
    IWorkspaceRepository workspaces,
    IUserWorkspaceRepository userWorkspaceRepository,
    IWorkspaceInventoryEditViewService viewService,
    IInventoryRepository inventory,
    IInventoryAllocationService inventoryAllocationService) : WorkspacePageModel
{
    #region Constants
    private const int NewInventoryId = 0;
    private const string DefaultInventoryStatus = "active";
    #endregion

    private readonly IWorkspaceRepository _workspaces = workspaces;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceInventoryEditViewService _viewService = viewService;
    private readonly IInventoryRepository _inventory = inventory;
    private readonly IInventoryAllocationService _inventoryAllocationService = inventoryAllocationService;

    public bool CanViewInventory { get; private set; }
    public bool CanEditInventory { get; private set; }
    public bool CanCreateInventory { get; private set; }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; set; }
    public List<Location> LocationOptions { get; set; } = [];

    [BindProperty]
    public Inventory Item { get; set; } = new Inventory();

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this._workspaces, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await this._viewService.BuildAsync(workspaceId, uid, id);
        this.CanViewInventory = viewData.CanViewInventory;
        this.CanEditInventory = viewData.CanEditInventory;
        this.CanCreateInventory = viewData.CanCreateInventory;

        if (this.EnsurePermissionOrForbid(this.CanViewInventory) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Item = viewData.ExistingItem ?? new Inventory { WorkspaceId = workspaceId, Status = DefaultInventoryStatus };
        this.LocationOptions = viewData.LocationOptions;
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this._workspaces, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;
        var viewData = await this._viewService.BuildAsync(workspaceId, uid, id);
        if (this.EnsureCreateOrEditPermission(id, viewData.CanCreateInventory, viewData.CanEditInventory) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (!this.ModelState.IsValid)
        {
            this.LocationOptions = viewData.LocationOptions;
            return this.Page();
        }

        this.Item.WorkspaceId = workspaceId;
        try
        {
            if (id == NewInventoryId)
            {
                await this.CreateInventoryItemAsync(workspaceId, uid);
            }
            else
            {
                await this.UpdateInventoryItemAsync(workspaceId, id, uid);
            }
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
            var errorViewData = await this._viewService.BuildAsync(workspaceId, uid, id);
            this.LocationOptions = errorViewData.LocationOptions;
            return this.Page();
        }

        return this.RedirectToInventoryWithPreservedFilters(slug);
    }

    private async Task CreateInventoryItemAsync(int workspaceId, int userId)
    {
        var created = await this._inventoryAllocationService.RegisterInventoryItemAsync(workspaceId, new InventoryRegistrationRequest
        {
            Sku = this.Item.Sku?.Trim() ?? string.Empty,
            Name = this.Item.Name?.Trim() ?? string.Empty,
            Description = this.Item.Description,
            InitialQuantity = this.Item.Quantity,
            UnitCost = this.Item.Cost,
            LocationId = this.Item.LocationId
        }, userId);

        created.Status = this.Item.Status;
        await this._inventory.UpdateAsync(created);
    }

    private async Task UpdateInventoryItemAsync(int workspaceId, int inventoryId, int userId)
    {
        var updated = await this._inventoryAllocationService.UpdateInventoryDetailsAsync(workspaceId, inventoryId, new InventoryDetailsUpdateRequest
        {
            Sku = this.Item.Sku?.Trim() ?? string.Empty,
            Name = this.Item.Name?.Trim() ?? string.Empty,
            Description = this.Item.Description,
            UnitCost = this.Item.Cost
        }, userId);

        updated.Quantity = this.Item.Quantity;
        updated.LocationId = this.Item.LocationId;
        updated.Status = this.Item.Status;
        await this._inventory.UpdateAsync(updated);
    }

    private IActionResult RedirectToInventoryWithPreservedFilters(string slug)
    {
        var queryQ = this.Request.Query["Query"].ToString();
        var locationQ = this.Request.Query["LocationId"].ToString();
        var pageQ = this.Request.Query["PageNumber"].ToString();
        return this.Redirect($"/workspaces/{slug}/inventory?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&LocationId={Uri.EscapeDataString(locationQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
    }
}

