namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize]
public class IndexModel() : PageModel
{
    public void OnGet([FromServices] IAppContext appContext)
    {
        if (this.Request.Path == "/")
        {
            if (appContext.CurrentUser != null)
            {
                this.Response.Redirect("/workspaces");
            }
            else
            {
                this.Response.Redirect("/login");
            }
        }
    }
}
