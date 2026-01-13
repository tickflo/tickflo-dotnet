using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Inventory;
namespace Tickflo.Web.Pages.Workspaces
{
    [Authorize]
    public class InventoryEditModel : WorkspacePageModel
    {
private readonly IWorkspaceRepository _workspaces;
    private readonly IWorkspaceInventoryEditViewService _viewService;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryService _inventoryService;

    public InventoryEditModel(IWorkspaceRepository workspaces, IWorkspaceInventoryEditViewService viewService, IInventoryRepository inventory, IInventoryService inventoryService)
    {
        _workspaces = workspaces;
        _viewService = viewService;
        _inventory = inventory;
        _inventoryService = inventoryService;
    }

    public bool CanViewInventory { get; private set; }
    public bool CanEditInventory { get; private set; }
    public bool CanCreateInventory { get; private set; }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; set; }
    public List<Location> LocationOptions { get; set; } = new();

    [BindProperty]
    public Inventory Item { get; set; } = new Inventory();

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;

        var workspaceId = Workspace.Id;
        if (!TryGetUserId(out var uid)) return Forbid();
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, id);
        CanViewInventory = viewData.CanViewInventory;
        CanEditInventory = viewData.CanEditInventory;
        CanCreateInventory = viewData.CanCreateInventory;
        
        if (EnsurePermissionOrForbid(CanViewInventory) is IActionResult permCheck) return permCheck;

        Item = viewData.ExistingItem ?? new Inventory { WorkspaceId = workspaceId, Status = "active" };
        LocationOptions = viewData.LocationOptions;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;

            var workspaceId = Workspace.Id;
            if (!TryGetUserId(out var uid)) return Forbid();
            
            var viewData = await _viewService.BuildAsync(workspaceId, uid, id);
            if (EnsureCreateOrEditPermission(id, viewData.CanCreateInventory, viewData.CanEditInventory) is IActionResult permCheck) return permCheck;

            if (!ModelState.IsValid)
            {
                LocationOptions = viewData.LocationOptions;
                return Page();
            }

            Item.WorkspaceId = workspaceId;
            try
            {
                if (id == 0)
                {
                    var created = await _inventoryService.CreateInventoryAsync(workspaceId, new CreateInventoryRequest
                    {
                        Sku = Item.Sku?.Trim() ?? string.Empty,
                        Name = Item.Name?.Trim() ?? string.Empty,
                        Description = Item.Description,
                        Quantity = Item.Quantity,
                        UnitPrice = Item.Cost,
                        LocationId = Item.LocationId
                    });
                    created.Status = Item.Status;
                    await _inventory.UpdateAsync(created);
                }
                else
                {
                    var updated = await _inventoryService.UpdateInventoryAsync(workspaceId, id, new UpdateInventoryRequest
                    {
                        Sku = Item.Sku?.Trim() ?? string.Empty,
                        Name = Item.Name?.Trim() ?? string.Empty,
                        Description = Item.Description,
                        Quantity = Item.Quantity,
                        UnitPrice = Item.Cost,
                        LocationId = Item.LocationId
                    });
                    updated.Status = Item.Status;
                    await _inventory.UpdateAsync(updated);
                }
            }
            catch (InvalidOperationException ex)
            {
                SetErrorMessage(ex.Message);
                var errorViewData = await _viewService.BuildAsync(workspaceId, uid, id);
                LocationOptions = errorViewData.LocationOptions;
                return Page();
            }

            var queryQ = Request.Query["Query"].ToString();
            var locationQ = Request.Query["LocationId"].ToString();
            var pageQ = Request.Query["PageNumber"].ToString();
            var slugSafe = WorkspaceSlug;
            return Redirect($"/workspaces/{slugSafe}/inventory?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&LocationId={Uri.EscapeDataString(locationQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
        }
    }
}

