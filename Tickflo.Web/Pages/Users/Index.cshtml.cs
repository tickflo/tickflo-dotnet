using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUserRepository _users;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IndexModel(IUserRepository users, IHttpContextAccessor httpContextAccessor)
    {
        _users = users;
        _httpContextAccessor = httpContextAccessor;
    }

    public List<User> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        var uid = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uid, out var userId))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        var me = await _users.FindByIdAsync(userId);
        if (me?.SystemAdmin != true)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        Users = await _users.ListAsync();
    }
}
