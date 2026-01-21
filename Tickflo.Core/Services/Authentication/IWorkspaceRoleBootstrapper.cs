namespace Tickflo.Core.Services.Authentication;

public interface IWorkspaceRoleBootstrapper
{
    public Task BootstrapAdminAsync(int workspaceId, int creatorUserId);
}


