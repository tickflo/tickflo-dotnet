using System.Threading.Tasks;

namespace Tickflo.Core.Services.Auth;

public interface IWorkspaceRoleBootstrapper
{
    Task BootstrapAdminAsync(int workspaceId, int creatorUserId);
}
