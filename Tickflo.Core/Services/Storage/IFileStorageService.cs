using System;
using System.IO;
using System.Threading.Tasks;

namespace Tickflo.Core.Services.Storage;

/// <summary>
/// Interface for file and image storage operations using RustFS (S3-compatible).
/// Provides abstraction over S3 storage for images, documents, and other files.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage with optional compression.
    /// </summary>
    /// <param name="filePath">Relative path within the bucket (e.g., "user-data/123/avatar.jpg")</param>
    /// <param name="fileStream">Stream containing the file data</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="compress">Whether to compress the file (for images)</param>
    /// <returns>Public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(string filePath, Stream fileStream, string contentType, bool compress = false);

    /// <summary>
    /// Uploads an image file with automatic compression.
    /// </summary>
    /// <param name="imagePath">Relative path within the bucket (e.g., "workspace-data/456/logo.jpg")</param>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <param name="maxWidth">Maximum width for resizing (default 800)</param>
    /// <param name="maxHeight">Maximum height for resizing (default 600)</param>
    /// <param name="quality">JPEG quality 0-100 (default 80)</param>
    /// <returns>Public URL of the uploaded image</returns>
    Task<string> UploadImageAsync(string imagePath, Stream imageStream, int maxWidth = 800, int maxHeight = 600, long quality = 80);

    /// <summary>
    /// Generates a thumbnail from an image.
    /// </summary>
    /// <param name="imagePath">Relative path within the bucket for the original image</param>
    /// <param name="thumbnailPath">Relative path for the thumbnail output</param>
    /// <param name="width">Thumbnail width</param>
    /// <param name="height">Thumbnail height</param>
    /// <returns>Public URL of the thumbnail</returns>
    Task<string> GenerateThumbnailAsync(string imagePath, string thumbnailPath, int width = 256, int height = 256);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    /// <param name="filePath">Relative path of the file to download</param>
    /// <returns>Stream containing the file data</returns>
    Task<Stream> DownloadFileAsync(string filePath);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="filePath">Relative path of the file to delete</param>
    /// <returns>True if successful, false if file not found</returns>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    /// <param name="filePath">Relative path of the file to check</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// Gets metadata about a file.
    /// </summary>
    /// <param name="filePath">Relative path of the file</param>
    /// <returns>File metadata including size and content type</returns>
    Task<FileMetadata?> GetFileMetadataAsync(string filePath);

    /// <summary>
    /// Gets the public URL for a file in storage.
    /// </summary>
    /// <param name="filePath">Relative path of the file</param>
    /// <returns>Public URL</returns>
    string GetFileUrl(string filePath);

    /// <summary>
    /// Lists all files under a given prefix path.
    /// </summary>
    /// <param name="prefix">Prefix path to list files under</param>
    /// <returns>List of file paths</returns>
    Task<IReadOnlyList<string>> ListFilesAsync(string prefix);
}

/// <summary>
/// Metadata about a stored file.
/// </summary>
public class FileMetadata
{
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string ETag { get; set; } = string.Empty;
}


