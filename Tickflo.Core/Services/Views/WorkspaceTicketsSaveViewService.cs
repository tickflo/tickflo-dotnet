namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Tickets;

public class WorkspaceTicketsSaveViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    ITicketManagementService ticketService) : IWorkspaceTicketsSaveViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly ITicketManagementService _ticketService = ticketService;

    public async Task<WorkspaceTicketsSaveViewData> BuildAsync(int workspaceId, int userId, bool isNew, Ticket? existing = null)
    {
        var data = new WorkspaceTicketsSaveViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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
                data.CanAccessTicket = await this._ticketService.CanUserAccessTicketAsync(existing, userId, workspaceId, isAdmin);
            }
            else
            {
                data.CanAccessTicket = true;
            }
        }

        return data;
    }
}



