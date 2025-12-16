using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IUserRepository _users;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DetailsModel(IUserRepository users, IHttpContextAccessor httpContextAccessor)
    {
        _users = users;
        _httpContextAccessor = httpContextAccessor;
    }

    public new User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var uid = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uid, out var userId))
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
