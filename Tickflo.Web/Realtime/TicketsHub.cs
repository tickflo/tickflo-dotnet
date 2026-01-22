namespace Tickflo.Web.Realtime;

using Microsoft.AspNetCore.SignalR;

public class TicketsHub : Hub
{
    public override Task OnConnectedAsync() => base.OnConnectedAsync();

    public async Task JoinWorkspace(string slug) => await this.Groups.AddToGroupAsync(this.Context.ConnectionId, WorkspaceGroup(slug));

    public static string WorkspaceGroup(string slug) => $"workspace:{slug}";
}
