namespace Tickflo.Web.Realtime;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;

[Authorize]
public class TicketsHub(TickfloDbContext dbContext) : Hub
{
    private readonly TickfloDbContext dbContext = dbContext;

    public override Task OnConnectedAsync() => base.OnConnectedAsync();

    public async Task JoinWorkspace(string slug)
    {
        // Verify the user is a member of the workspace identified by slug
        var userIdClaim = this.Context.User?.FindFirst("userId")?.Value
            ?? this.Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            // Unauthorized — silently reject
            return;
        }

        var workspace = await this.dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == slug);

        if (workspace == null)
        {
            return;
        }

        var hasAccess = await this.dbContext.UserWorkspaces
            .AnyAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspace.Id && uw.Accepted);

        if (!hasAccess)
        {
            return;
        }

        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, WorkspaceGroup(slug));
    }

    public static string WorkspaceGroup(string slug) => $"workspace:{slug}";
}
