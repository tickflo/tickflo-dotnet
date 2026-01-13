using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class InventoryModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IWorkspaceInventoryViewService _viewService;

    public InventoryModel(
        IWorkspaceRepository workspaces,
        IInventoryRepository inventoryRepo,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService,
        IWorkspaceInventoryViewService viewService)
    {
        _workspaces = workspaces;
        _inventoryRepo = inventoryRepo;
        _currentUserService = currentUserService;
        _workspaceAccessService = workspaceAccessService;
        _viewService = viewService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public bool IsWorkspaceAdmin { get; private set; }
    public bool CanCreateInventory { get; private set; }
    public bool CanEditInventory { get; private set; }
    public Workspace? Workspace { get; set; }
    public IEnumerable<Inventory> Items { get; set; } = new List<Inventory>();

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaces, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
        IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        CanCreateInventory = viewData.CanCreateInventory;
        CanEditInventory = viewData.CanEditInventory;

        // Apply client-side filtering on loaded items
        Items = viewData.Items;
        if (!string.IsNullOrWhiteSpace(Query))
        {
            Items = Items.Where(i => i.Name?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        }
        if (!string.IsNullOrWhiteSpace(Status))
        {
            Items = Items.Where(i => i.Status == Status).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostArchiveAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null)
        {
            return NotFound();
        }

        if (!_currentUserService.TryGetUserId(User, out var uid))
        {
            return Forbid();
        }

        // Use service to check permissions
        bool allowed = await _workspaceAccessService.CanUserPerformActionAsync(Workspace.Id, uid, "inventory", "edit");
        if (!allowed)
        {
            return Forbid();
        }

        var item = await _inventoryRepo.FindAsync(Workspace.Id, id);
        if (item == null)
        {
            return NotFound();
        }

        item.Status = "archived";
        await _inventoryRepo.UpdateAsync(item);

        SetSuccessMessage("Inventory item archived.");
        return Redirect($"/workspaces/{Workspace.Slug}/inventory");
    }

    public async Task<IActionResult> OnPostRestoreAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null)
        {
            return NotFound();
        }

        if (!_currentUserService.TryGetUserId(User, out var uid))
        {
            return Forbid();
        }

        // Use service to check permissions
        bool allowed = await _workspaceAccessService.CanUserPerformActionAsync(Workspace.Id, uid, "inventory", "edit");
        if (!allowed)
        {
            return Forbid();
        }

        var item = await _inventoryRepo.FindAsync(Workspace.Id, id);
        if (item == null)
        {
            return NotFound();
        }

        item.Status = "active";
        await _inventoryRepo.UpdateAsync(item);

        SetSuccessMessage("Inventory item restored.");
        return Redirect($"/workspaces/{Workspace.Slug}/inventory");
    }
}
