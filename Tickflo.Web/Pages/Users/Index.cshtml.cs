using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUserRepository _users;

    public IndexModel(IUserRepository users)
    {
        _users = users;
    }

    public List<User> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (!TryGetUserId(out var userId))
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

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
