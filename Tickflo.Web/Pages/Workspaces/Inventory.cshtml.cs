using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class InventoryModel : WorkspacePageModel
{
    #region Constants
    private const string InventorySection = "inventory";
    private const string EditAction = "edit";
    private const string ArchivedStatus = "archived";
    private const string ActiveStatus = "active";
    private const string ItemArchivedMessage = "Inventory item archived.";
    private const string ItemRestoredMessage = "Inventory item restored.";
    #endregion

    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IWorkspaceInventoryViewService _viewService;

    public InventoryModel(
        IWorkspaceRepository workspaces,
        IUserWorkspaceRepository userWorkspaceRepo,
        IInventoryRepository inventoryRepo,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService,
        IWorkspaceInventoryViewService viewService)
    {
        _workspaces = workspaces;
        _userWorkspaceRepo = userWorkspaceRepo;
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
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaces, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace!.Id, uid);
        IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        CanCreateInventory = viewData.CanCreateInventory;
        CanEditInventory = viewData.CanEditInventory;

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
        
        if (await AuthorizeInventoryEditAsync(slug) is IActionResult authResult)
            return authResult;
        
        var item = await _inventoryRepo.FindAsync(Workspace!.Id, id);
        if (item == null) return NotFound();

        await UpdateInventoryStatusAsync(item, ArchivedStatus);
        SetSuccessMessage(ItemArchivedMessage);
        
        return RedirectToInventoryPage();
    }

    public async Task<IActionResult> OnPostRestoreAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        
        if (await AuthorizeInventoryEditAsync(slug) is IActionResult authResult)
            return authResult;
        
        var item = await _inventoryRepo.FindAsync(Workspace!.Id, id);
        if (item == null) return NotFound();

        await UpdateInventoryStatusAsync(item, ActiveStatus);
        SetSuccessMessage(ItemRestoredMessage);
        
        return RedirectToInventoryPage();
    }

    private async Task<IActionResult?> AuthorizeInventoryEditAsync(string slug)
    {
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!_currentUserService.TryGetUserId(User, out var uid))
            return Forbid();

        bool allowed = await _workspaceAccessService.CanUserPerformActionAsync(
            Workspace.Id, uid, InventorySection, EditAction);
        
        if (!allowed) return Forbid();
        
        return null;
    }

    private async Task UpdateInventoryStatusAsync(Inventory item, string status)
    {
        item.Status = status;
        await _inventoryRepo.UpdateAsync(item);
    }

    private IActionResult RedirectToInventoryPage()
    {
        return Redirect($"/workspaces/{Workspace!.Slug}/inventory");
    }
}

