using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUserService;

    public DetailsModel(IUserRepository users, ICurrentUserService currentUserService)
    {
        _users = users;
        _currentUserService = currentUserService;
    }

    public new User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!_currentUserService.TryGetUserId(base.User, out var userId))
            return Forbid();
        var me = await _users.FindByIdAsync(userId);
        if (me?.SystemAdmin != true)
            return Forbid();
        var found = await _users.FindByIdAsync(id);
        if (found == null)
            return NotFound();
        User = found;
        return Page();
    }
}
