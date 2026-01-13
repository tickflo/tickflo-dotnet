using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Tickflo.Web.Realtime;

public class TicketsHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task JoinWorkspace(string slug)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, WorkspaceGroup(slug));
    }

    public static string WorkspaceGroup(string slug) => $"workspace:{slug}";
}
