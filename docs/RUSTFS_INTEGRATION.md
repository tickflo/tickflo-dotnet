# RustFS File and Image Storage Integration

## Overview

This document describes the complete RustFS (S3-compatible) file and image storage system implementation in Tickflo. RustFS provides a self-hosted, Rust-based S3-compatible storage solution that integrates seamlessly with the application.

## Architecture

### Components

#### 1. **IFileStorageService** (`Tickflo.Core.Services.IFileStorageService`)
Base interface for all file storage operations.

- `UploadFileAsync()` - Generic file upload with optional compression
- `UploadImageAsync()` - Image upload with automatic compression and resizing
- `GenerateThumbnailAsync()` - Create thumbnails from stored images
- `DownloadFileAsync()` - Retrieve files from storage
- `DeleteFileAsync()` - Remove files from storage
- `FileExistsAsync()` - Check file existence
- `GetFileMetadataAsync()` - Retrieve file metadata
- `GetFileUrl()` - Get public URL for a file
- `ListFilesAsync()` - List files by prefix

#### 2. **RustFSStorageService** (`Tickflo.Core.Services.RustFSStorageService`)
Implementation of IFileStorageService using AWS S3 SDK.

**Features:**
- Automatic image compression using ImageSharp
- Configurable compression quality and dimensions
- Error handling and logging
- S3 ACL management (PublicRead for accessible files)

#### 3. **IImageStorageService** (`Tickflo.Core.Services.IImageStorageService`)
Specialized service for image-specific operations.

**Specialized Methods:**
- `UploadUserAvatarAsync()` - Upload user profile avatars (256x256)
- `UploadWorkspaceLogoAsync()` - Upload workspace branding (512x512)
- `UploadWorkspaceBannerAsync()` - Upload workspace banners (1920x1080)
- `UploadDocumentImageAsync()` - Upload document/ticket images (1200x900)
- `IsValidImage()` - Validate image file format
- `GetAllowedImageExtensions()` - List allowed image types

**Supported Formats:**
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- WebP (.webp)

#### 4. **RustFSImageStorageService** (`Tickflo.Core.Services.RustFSImageStorageService`)
Implementation of IImageStorageService.

**Key Features:**
- Organized directory structure by image type
- Automatic format conversion to JPEG
- Quality optimization per image type
- File metadata validation

#### 5. **FileStorage Entity** (`Tickflo.Core.Entities.FileStorage`)
Database entity for tracking stored files.

```csharp
public class FileStorage
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? UserId { get; set; }
    public string Path { get; set; }           // RustFS path
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string FileType { get; set; }       // "image", "document"
    public string Category { get; set; }       // "user-avatar", "workspace-logo"
    public string PublicUrl { get; set; }
    public bool IsPublic { get; set; }
    public bool IsArchived { get; set; }
    // ... timestamps and user tracking fields
}
```

#### 6. **IFileStorageRepository** (`Tickflo.Core.Data.IFileStorageRepository`)
Data access for FileStorage records.

**Methods:**
- CRUD operations (Create, Update, Find, Delete)
- Query by workspace, user, category, type
- Soft delete (archive) and hard delete
- Storage statistics (used space, file count)

#### 7. **FilesController** (`Tickflo.Web.Controllers.FilesController`)
REST API endpoints for file operations.

**Endpoints:**
- `POST /api/files/upload/{workspaceId}` - Upload file
- `POST /api/files/upload-image/{workspaceId}` - Upload image
- `GET /api/files/download/{fileId}` - Download file
- `DELETE /api/files/{fileId}` - Delete file
- `GET /api/files/list/{workspaceId}` - List workspace files
- `GET /api/files/storage-info/{workspaceId}` - Get storage usage

## Directory Structure

Files are organized in RustFS using the following structure:

```
tickflo-bucket/
├── user-data/
│   └── {userId}/
│       └── avatar.jpg
├── workspace-data/
│   └── {workspaceId}/
│       ├── logo.jpg
│       └── banner.jpg
├── workspace-images/
│   └── {workspaceId}/
│       ├── document/
│       ├── attachment/
│       └── ...
├── workspace-documents/
│   └── {workspaceId}/
│       └── {documentPath}/
├── workspace-uploads/
│   └── {workspaceId}/
│       └── {fileName}
```

## Image Optimization

All images are automatically optimized on upload:

| Image Type | Max Width | Max Height | Quality | Format |
|------------|-----------|-----------|---------|--------|
| Avatar | 256 | 256 | 85 | JPEG |
| Logo | 512 | 512 | 85 | JPEG |
| Banner | 1920 | 1080 | 80 | JPEG |
| Document/General | 1200 | 900 | 80 | JPEG |

## File Size Limits

Configuration via environment variables:
- `MAX_FILE_SIZE_MB=50` - General file upload limit
- `MAX_IMAGE_SIZE_MB=10` - Image upload limit
- `MAX_WORKSPACE_STORAGE_GB=100` - Workspace total storage limit

## Configuration

### Environment Variables (.env)

```bash
# RustFS/S3 Configuration
S3_ENDPOINT=http://localhost:9000
S3_ACCESS_KEY=admin
S3_SECRET_KEY=password
S3_BUCKET=tickflo
S3_REGION=us-east-1

# Storage Limits
MAX_FILE_SIZE_MB=50
MAX_IMAGE_SIZE_MB=10
MAX_WORKSPACE_STORAGE_GB=100
```

### Docker Compose Setup

The `compose.yml` includes RustFS service:

```yaml
services:
  s3:
    image: "rustfs/rustfs:latest"
    restart: always
    ports:
      - "9000:9000"    # S3 API
      - "9001:9001"    # Console UI
    environment:
      RUSTFS_ACCESS_KEY: admin
      RUSTFS_SECRET_KEY: password
    volumes:
      - "./rustfs-data:/data"
```

Start RustFS with Docker Compose:
```bash
docker-compose up -d
```

## Usage Examples

### Upload User Avatar

```csharp
var userId = 123;
using var imageStream = new FileStream("avatar.jpg", FileMode.Open);
var url = await imageStorageService.UploadUserAvatarAsync(userId, imageStream);
// Returns: http://localhost:9000/tickflo/user-data/123/avatar.jpg
```

### Upload Document Image

```csharp
var workspaceId = 456;
using var imageStream = file.OpenReadStream();
var url = await imageStorageService.UploadDocumentImageAsync(
    workspaceId, 
    "ticket-789/screenshot", 
    imageStream
);
// Returns: http://localhost:9000/tickflo/workspace-documents/456/ticket-789/screenshot.jpg
```

### Upload Generic File

```csharp
var workspaceId = 456;
using var fileStream = file.OpenReadStream();
var url = await fileStorageService.UploadFileAsync(
    $"workspace-uploads/{workspaceId}/report.pdf",
    fileStream,
    "application/pdf",
    false
);
```

### Get Storage Usage

```csharp
var usedBytes = await fileRepository.GetWorkspaceStorageUsedAsync(workspaceId);
var fileCount = await fileRepository.GetWorkspaceFileCountAsync(workspaceId);
```

### Delete File

```csharp
var deleted = await fileStorageService.DeleteFileAsync("user-data/123/avatar.jpg");
```

## Service Registration

Services are registered in `Program.cs`:

```csharp
// RustFS file and image storage services
builder.Services.AddScoped<IFileStorageService, RustFSStorageService>();
builder.Services.AddScoped<IImageStorageService, RustFSImageStorageService>();
builder.Services.AddScoped<IFileStorageRepository, FileStorageRepository>();
```

## Database Migration

A database migration is required to create the FileStorage table:

```sql
CREATE TABLE file_storages (
    id SERIAL PRIMARY KEY,
    workspace_id INTEGER NOT NULL,
    user_id INTEGER,
    path VARCHAR(512) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    size BIGINT NOT NULL,
    file_type VARCHAR(50) NOT NULL,
    category VARCHAR(50) NOT NULL,
    description TEXT,
    public_url TEXT NOT NULL,
    is_public BOOLEAN DEFAULT false,
    is_archived BOOLEAN DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INTEGER,
    updated_at TIMESTAMP,
    updated_by_user_id INTEGER,
    deleted_at TIMESTAMP,
    deleted_by_user_id INTEGER,
    metadata TEXT,
    ticket_id INTEGER,
    contact_id INTEGER,
    related_entity_type VARCHAR(50),
    related_entity_id INTEGER,
    CONSTRAINT fk_workspace FOREIGN KEY (workspace_id) REFERENCES workspaces(id),
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_file_storages_workspace_created ON file_storages(workspace_id, created_at);
CREATE INDEX idx_file_storages_workspace_category ON file_storages(workspace_id, category);
CREATE INDEX idx_file_storages_related_entity ON file_storages(related_entity_type, related_entity_id);
CREATE INDEX idx_file_storages_path ON file_storages(path);
```

## API Examples

### Upload File

```bash
curl -X POST http://localhost:3000/api/files/upload/1 \
  -H "Authorization: Bearer {token}" \
  -F "file=@document.pdf"

# Response
{
  "id": 123,
  "url": "http://localhost:9000/tickflo/workspace-uploads/1/abc123.pdf",
  "fileName": "document.pdf"
}
```

### Upload Image

```bash
curl -X POST http://localhost:3000/api/files/upload-image/1 \
  -H "Authorization: Bearer {token}" \
  -F "image=@screenshot.png" \
  -F "category=document"

# Response
{
  "id": 124,
  "url": "http://localhost:9000/tickflo/workspace-images/1/document/xyz789.jpg",
  "fileName": "screenshot.png"
}
```

### List Files

```bash
curl http://localhost:3000/api/files/list/1?take=50&skip=0 \
  -H "Authorization: Bearer {token}"

# Response
{
  "files": [
    {
      "id": 123,
      "fileName": "document.pdf",
      "publicUrl": "http://localhost:9000/...",
      "size": 1024000,
      "contentType": "application/pdf",
      "category": "workspace-upload",
      "fileType": "document",
      "createdAt": "2026-01-09T10:30:00Z"
    }
  ],
  "total": 45,
  "take": 50,
  "skip": 0
}
```

### Get Storage Info

```bash
curl http://localhost:3000/api/files/storage-info/1 \
  -H "Authorization: Bearer {token}"

# Response
{
  "usedBytes": 52428800,
  "usedMB": 50.0,
  "fileCount": 12,
  "maxBytes": 52428800,
  "maxMB": 50
}
```

## Integration with Existing Features

### User Avatar Upload (Existing)

The new system can replace the existing avatar upload:

```csharp
// Old way
await _s3Client.PutObjectAsync(/* ... */);

// New way (recommended)
var url = await _imageStorageService.UploadUserAvatarAsync(userId, stream);
```

### Ticket Attachments

For ticket attachments, use:

```csharp
var url = await _imageStorageService.UploadDocumentImageAsync(
    workspaceId,
    $"ticket-{ticketId}/attachment-{attachmentId}",
    imageStream
);

// Track in database
await _fileRepository.CreateAsync(new FileStorage
{
    WorkspaceId = workspaceId,
    TicketId = ticketId,
    RelatedEntityType = "Ticket",
    RelatedEntityId = ticketId,
    // ... other properties
});
```

## Performance Considerations

1. **Image Compression**: Reduces bandwidth and storage by ~70-80%
2. **Lazy Loading**: Images are resized on first access, then cached
3. **CDN Ready**: URLs can be proxied through a CDN for global distribution
4. **Batch Operations**: Use `ListFilesAsync()` with pagination for large collections

## Security

1. **Access Control**: All endpoints require authentication via Bearer token
2. **File Validation**: Image files are validated by magic bytes
3. **Soft Deletes**: Files are archived (soft deleted) by default for recovery
4. **ACL Management**: Files use S3 CannedACL.PublicRead for web access
5. **Workspace Isolation**: Files are scoped to workspace for multi-tenancy

## Monitoring and Logging

All operations are logged with:
- Operation type (upload, download, delete)
- User ID and Workspace ID
- File path and size
- Timestamps
- Error details

View logs:
```bash
# Docker logs
docker-compose logs -f s3

# Application logs
grep -i "file\|image\|storage" logs/app.log
```

## Troubleshooting

### RustFS Connection Issues

```
error: "Error uploading file: Connection refused"
```

**Solution:** Verify RustFS is running:
```bash
docker-compose ps
curl http://localhost:9000/minio/health/live
```

### Image Compression Errors

```
error: "Error uploading image: Unable to load image"
```

**Solution:** Ensure image file is valid and not corrupted:
```csharp
if (!_imageStorageService.IsValidImage(stream))
    throw new InvalidOperationException("Invalid image file");
```

### Storage Quota Exceeded

```
error: "Workspace storage quota exceeded"
```

**Solution:** Check usage and clean up old files:
```csharp
var used = await _fileRepository.GetWorkspaceStorageUsedAsync(workspaceId);
var maxBytes = 100 * 1024 * 1024 * 1024; // 100 GB
if (used + fileSize > maxBytes)
    throw new InvalidOperationException("Storage quota exceeded");
```

## Future Enhancements

1. **Caching Layer**: Add Redis caching for file metadata
2. **Video Support**: Extend to support video uploads with transcoding
3. **Virus Scanning**: Integration with ClamAV or similar
4. **Batch Uploads**: Support for multipart uploads
5. **File Versioning**: Track file versions and history
6. **Advanced Search**: Full-text search for document content
7. **Expiration Policies**: Auto-delete files after retention period

## References

- [RustFS Documentation](https://docs.rustfs.dev)
- [AWS S3 SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/)
- [ImageSharp Documentation](https://docs.sixlabors.com/articles/imagesharp/index.html)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
