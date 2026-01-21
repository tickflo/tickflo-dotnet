namespace Tickflo.Web.Controllers;

using Amazon.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Config;

[Authorize]
[Route("users/{id}/avatar")]
public class UserAvatarController(TickfloConfig config, IAmazonS3 s3Client) : Controller
{
    private readonly TickfloConfig _config = config;
    private readonly IAmazonS3 _s3Client = s3Client;

    [HttpGet]
    public async Task<IActionResult> GetAvatar(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return this.NotFound();
        }

        var bucket = this._config.S3Bucket;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            return this.NotFound();
        }

        var key = $"user-data/{id}/avatar.jpg";
        try
        {
            using var response = await this._s3Client.GetObjectAsync(bucket, key);
            await using var stream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(stream);
            stream.Position = 0;

            var contentType = response.Headers.ContentType ?? "image/jpeg";
            return this.File(stream.ToArray(), contentType);
        }
        catch (AmazonS3Exception)
        {
            // If S3 is unavailable or account not signed up, return NotFound so the app doesn't crash.
            return this.NotFound();
        }
        catch
        {
            return this.NotFound();
        }
    }
}
