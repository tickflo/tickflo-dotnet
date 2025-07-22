using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tickflo.Web.Pages;

[Authorize]
public class TestModel() : PageModel
{
    public void OnGet()
    {
        var userId = ClaimTypes.NameIdentifier;
    }
}

