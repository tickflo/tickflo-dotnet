using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Config;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;

namespace Tickflo.Web.Controllers
{
    [Authorize]
    [Route("users/{id}/avatar")]
    public class UserAvatarController : Controller
    {
        private readonly TickfloConfig _config;
        private readonly IAmazonS3 _s3Client;

        public UserAvatarController(TickfloConfig config, IAmazonS3 s3Client)
        {
            _config = config;
            _s3Client = s3Client;
        }

        [HttpGet]
        public async Task<IActionResult> GetAvatar(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var bucket = _config.S3_BUCKET;
            if (string.IsNullOrWhiteSpace(bucket))
            {
                return NotFound();
            }

            var key = $"user-data/{id}/avatar.jpg";
            try
            {
                using var response = await _s3Client.GetObjectAsync(bucket, key);
                await using var stream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(stream);
                stream.Position = 0;

                var contentType = response.Headers.ContentType ?? "image/jpeg";
                return File(stream.ToArray(), contentType);
            }
            catch (AmazonS3Exception)
            {
                // If S3 is unavailable or account not signed up, return NotFound so the app doesn't crash.
                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
