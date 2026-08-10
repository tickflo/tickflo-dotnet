namespace Tickflo.Web.Controllers;

using Amazon.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Config;

[Authorize]
[Route("users/{id}/avatar")]
public class UserAvatarController(TickfloConfig config, IAmazonS3 amazonS3) : Controller
{
    private readonly TickfloConfig config = config;
    private readonly IAmazonS3 amazonS3 = amazonS3;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAvatar(int id)
    {
        // Only allow users to access their own avatar
        var userIdClaim = this.User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
        {
            return this.Forbid();
        }

        var bucket = this.config.S3Bucket;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            return this.NotFound();
        }

        var key = $"user-data/{id}/avatar.jpg";
        try
        {
            using var response = await this.amazonS3.GetObjectAsync(bucket, key);
            await using var stream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(stream);
            stream.Position = 0;

            var contentType = response.Headers.ContentType ?? "image/jpeg";
            return this.File(stream.ToArray(), contentType);
        }
        catch
        {
            return this.Redirect("/img/avatar.png");
        }
    }
}
