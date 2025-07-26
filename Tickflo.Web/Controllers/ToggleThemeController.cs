using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tickflo.Web.Controllers
{
    [AllowAnonymous]
    [Route("toggle-theme")]
    public class ToggleThemeController : Controller
    {
        [HttpPost]
        public IActionResult ToggleTheme()
        {
            var theme = Request.Cookies["theme"];
            var newTheme = theme == "dark" ? "light" : "dark";
            Response.Cookies.Append("theme", newTheme, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });

            return Redirect(Request.Headers.Referer.ToString() ?? "/");
        }
    }
}
