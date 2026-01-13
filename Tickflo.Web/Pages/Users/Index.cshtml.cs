using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUserService;

    public IndexModel(IUserRepository users, ICurrentUserService currentUserService)
    {
        _users = users;
        _currentUserService = currentUserService;
    }

    public List<User> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (!_currentUserService.TryGetUserId(User, out var userId))
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
