using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IUserRepository _users;

    public DetailsModel(IUserRepository users)
    {
        _users = users;
    }

    public new User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!TryGetUserId(out var userId))
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

    private bool TryGetUserId(out int userId)
    {
        var idValue = base.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
