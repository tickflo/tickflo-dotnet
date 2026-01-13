using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Tickets;

namespace Tickflo.Core.Services.Views;

public class WorkspaceTicketsSaveViewService : IWorkspaceTicketsSaveViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ITicketManagementService _ticketService;

    public WorkspaceTicketsSaveViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        ITicketManagementService ticketService)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _ticketService = ticketService;
    }

    public async Task<WorkspaceTicketsSaveViewData> BuildAsync(int workspaceId, int userId, bool isNew, Ticket? existing = null)
    {
        var data = new WorkspaceTicketsSaveViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanCreateTickets = true;
            data.CanEditTickets = true;
            data.CanAccessTicket = true;
        }
        else
        {
            if (eff.TryGetValue("tickets", out var tp))
            {
                data.CanCreateTickets = tp.CanCreate;
                data.CanEditTickets = tp.CanEdit;
            }

            // For existing tickets, also check scope access
            if (!isNew && existing != null)
            {
                data.CanAccessTicket = await _ticketService.CanUserAccessTicketAsync(existing, userId, workspaceId, isAdmin);
            }
            else
            {
                data.CanAccessTicket = true;
            }
        }

        return data;
    }
}



