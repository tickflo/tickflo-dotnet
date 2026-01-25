namespace Tickflo.Web.Services;

using Amazon.S3;
using Amazon.S3.Model;
using Tickflo.Core.Config;

using Tickflo.Core.Services.Storage;

/// <summary>
/// Implementation of IFileStorageService using RustFS (S3-compatible storage).
/// Handles file uploads, downloads, and image processing with automatic compression.
/// </summary>
public class RustFSStorageService(IAmazonS3 s3Client, TickfloConfig config, ILogger<RustFSStorageService> logger) : IFileStorageService
{
    private readonly IAmazonS3 amazonS3 = s3Client;
    private readonly TickfloConfig config = config;
    private readonly ILogger<RustFSStorageService> logger = logger;

    public async Task<string> UploadFileAsync(string filePath, Stream fileStream, string contentType, bool compress = false)
    {
        try
        {
            if (compress && contentType.StartsWith("image/", StringComparison.Ordinal))
            {
                return await this.UploadImageAsync(filePath, fileStream, 800, 600, 80);
            }

            var putRequest = new PutObjectRequest
            {
                BucketName = this.config.S3Bucket,
                Key = filePath,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            await this.amazonS3.PutObjectAsync(putRequest);

            this.logger.LogInformation($"File uploaded successfully: {filePath}");
            return this.GetFileUrl(filePath);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error uploading file: {filePath}");
            throw;
        }
    }

    public async Task<string> UploadImageAsync(string imagePath, Stream imageStream, int maxWidth = 800, int maxHeight = 600, long quality = 80)
    {
        try
        {
            using var compressedStream = new MemoryStream();

            // Compress and resize the image
            imageStream.Position = 0;
            Utils.ImageCompressor.CompressAndSave(imageStream, compressedStream, maxWidth, maxHeight, quality);
            compressedStream.Position = 0;

            var putRequest = new PutObjectRequest
            {
                BucketName = this.config.S3Bucket,
                Key = imagePath,
                InputStream = compressedStream,
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.PublicRead
            };

            await this.amazonS3.PutObjectAsync(putRequest);

            this.logger.LogInformation($"Image uploaded and compressed: {imagePath}");
            return this.GetFileUrl(imagePath);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error uploading image: {imagePath}");
            throw;
        }
    }

    public async Task<string> GenerateThumbnailAsync(string imagePath, string thumbnailPath, int width = 256, int height = 256)
    {
        try
        {
            // Download the original image
            var getRequest = new GetObjectRequest
            {
                BucketName = this.config.S3Bucket,
                Key = imagePath
            };

            using var response = await this.amazonS3.GetObjectAsync(getRequest);
            using var originalStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(originalStream);
            originalStream.Position = 0;

            // Create thumbnail
            using var thumbnailStream = new MemoryStream();
            Utils.ImageCompressor.CompressAndSave(originalStream, thumbnailStream, width, height, 80);
            thumbnailStream.Position = 0;

            // Upload thumbnail
            var putRequest = new PutObjectRequest
            {
                BucketName = this.config.S3Bucket,
                Key = thumbnailPath,
                InputStream = thumbnailStream,
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.PublicRead
            };

            await this.amazonS3.PutObjectAsync(putRequest);

            this.logger.LogInformation($"Thumbnail generated: {thumbnailPath}");
            return this.GetFileUrl(thumbnailPath);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error generating thumbnail: {imagePath}");
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = this.config.S3Bucket,
                Key = filePath
            };

            var response = await this.amazonS3.GetObjectAsync(getRequest);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            this.logger.LogInformation($"File downloaded: {filePath}");
            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            this.logger.LogWarning($"File not found: {filePath}");
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error downloading file: {filePath}");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = this.config.S3Bucket,
                Key = filePath
            };

            await this.amazonS3.DeleteObjectAsync(deleteRequest);

            this.logger.LogInformation($"File deleted: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error deleting file: {filePath}");
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = this.config.S3Bucket,
                Key = filePath
            };

            await this.amazonS3.GetObjectMetadataAsync(metadataRequest);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error checking file existence: {filePath}");
            return false;
        }
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string filePath)
    {
        try
        {
            var metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = this.config.S3Bucket,
                Key = filePath
            };

            var response = await this.amazonS3.GetObjectMetadataAsync(metadataRequest);

            return new FileMetadata
            {
                Path = filePath,
                Size = response.ContentLength,
                ContentType = response.Headers.ContentType,
                LastModified = response.LastModified ?? DateTime.UtcNow,
                ETag = response.ETag
            };
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error getting file metadata: {filePath}");
            return null;
        }
    }

    public string GetFileUrl(string filePath)
    {
        // Construct the public URL based on the S3 endpoint and bucket
        var endpoint = this.config.S3EndPoint.TrimEnd('/');
        return $"{endpoint}/{this.config.S3Bucket}/{filePath}";
    }

    public async Task<IReadOnlyList<string>> ListFilesAsync(string prefix)
    {
        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = this.config.S3Bucket,
                Prefix = prefix
            };

            var response = await this.amazonS3.ListObjectsV2Async(listRequest);
            var files = response.S3Objects.Select(obj => obj.Key).ToList();

            this.logger.LogInformation($"Listed {files.Count} files with prefix: {prefix}");
            return files.AsReadOnly();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error listing files with prefix: {prefix}");
            throw;
        }
    }
}

