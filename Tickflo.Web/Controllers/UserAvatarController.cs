using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
            var bucket = _config.S3_BUCKET;
            var key = $"user-data/{id}/avatar.jpg";
            try
            {
                var response = await _s3Client.GetObjectAsync(bucket, key);
                using var stream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(stream);
                stream.Position = 0;
                return File(stream.ToArray(), response.Headers["Content-Type"] ?? "image/jpeg");
            }
            catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
