using System.Threading.Tasks;

namespace Tickflo.Core.Services.Authentication;

public interface IWorkspaceRoleBootstrapper
{
    Task BootstrapAdminAsync(int workspaceId, int creatorUserId);
}


