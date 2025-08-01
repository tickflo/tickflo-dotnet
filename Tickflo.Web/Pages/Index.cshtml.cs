using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tickflo.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        if (Request.Path == "/")
        {
            Response.Redirect("/login");
        }
    }
}
