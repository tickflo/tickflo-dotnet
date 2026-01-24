namespace Tickflo.Web;

using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IAppContext
{
    public User? CurrentUser { get; set; }
}

public class AppContext : IAppContext
{
    public User? CurrentUser { get; set; }
}

public class AppContextMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;
    public async Task InvokeAsync(HttpContext context, IAppContext appContext, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await userRepository.FindByIdAsync(userId);
                appContext.CurrentUser = user;
            }
        }

        await this.next(context);
    }
}
