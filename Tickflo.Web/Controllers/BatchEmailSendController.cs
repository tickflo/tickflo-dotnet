namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Jobs;

[Authorize]
[Route("api/send-emails")]
public class BatchEmailSendController(IBatchEmailSendService batchEmailSendService) : Controller
{
    private readonly IBatchEmailSendService batchEmailSendService = batchEmailSendService;

    [HttpGet]
    public async Task<IActionResult> SendEmails()
    {
        await this.batchEmailSendService.ProcessEmailQueueAsync();
        return this.Ok();
    }
}

