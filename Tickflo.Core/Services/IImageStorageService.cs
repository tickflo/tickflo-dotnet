using System;
using System.IO;
using System.Threading.Tasks;

namespace Tickflo.Core.Services;

/// <summary>
/// Specialized service for image handling with RustFS storage.
/// Provides utilities for avatar management, workspace logos, and general image handling.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Uploads a user avatar image.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <returns>URL of the uploaded avatar</returns>
    Task<string> UploadUserAvatarAsync(int userId, Stream imageStream);

    /// <summary>
    /// Deletes a user avatar.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteUserAvatarAsync(int userId);

    /// <summary>
    /// Gets the URL for a user avatar.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>URL of the avatar</returns>
    string GetUserAvatarUrl(int userId);

    /// <summary>
    /// Uploads a workspace logo image.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <returns>URL of the uploaded logo</returns>
    Task<string> UploadWorkspaceLogoAsync(int workspaceId, Stream imageStream);

    /// <summary>
    /// Deletes a workspace logo.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteWorkspaceLogoAsync(int workspaceId);

    /// <summary>
    /// Gets the URL for a workspace logo.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>URL of the logo</returns>
    string GetWorkspaceLogoUrl(int workspaceId);

    /// <summary>
    /// Uploads a workspace banner image.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <returns>URL of the uploaded banner</returns>
    Task<string> UploadWorkspaceBannerAsync(int workspaceId, Stream imageStream);

    /// <summary>
    /// Deletes a workspace banner.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteWorkspaceBannerAsync(int workspaceId);

    /// <summary>
    /// Gets the URL for a workspace banner.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>URL of the banner</returns>
    string GetWorkspaceBannerUrl(int workspaceId);

    /// <summary>
    /// Uploads a document or attachment image.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="documentPath">The path within the workspace (e.g., "ticket-123/attachment")</param>
    /// <param name="imageStream">Stream containing the image data</param>
    /// <returns>URL of the uploaded image</returns>
    Task<string> UploadDocumentImageAsync(int workspaceId, string documentPath, Stream imageStream);

    /// <summary>
    /// Deletes a document image.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="documentPath">The path within the workspace</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteDocumentImageAsync(int workspaceId, string documentPath);

    /// <summary>
    /// Gets the URL for a document image.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="documentPath">The path within the workspace</param>
    /// <returns>URL of the image</returns>
    string GetDocumentImageUrl(int workspaceId, string documentPath);

    /// <summary>
    /// Validates if a stream contains a valid image file.
    /// </summary>
    /// <param name="imageStream">Stream to validate</param>
    /// <returns>True if valid image</returns>
    bool IsValidImage(Stream imageStream);

    /// <summary>
    /// Gets allowed image extensions.
    /// </summary>
    /// <returns>Array of allowed extensions (e.g., ".jpg", ".png")</returns>
    string[] GetAllowedImageExtensions();
}
