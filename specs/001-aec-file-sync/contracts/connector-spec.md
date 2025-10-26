# Connector Interface Specification

**Version**: 1.0.0  
**Date**: 2025-10-26  
**Status**: Final

---

## Overview

The `IConnector` interface abstracts platform-specific operations for Autodesk Construction Cloud (ACC) Docs and Microsoft SharePoint Online. All connector implementations must support OAuth2-based authentication, delta token checkpointing, and standardized error handling.

---

## IConnector Interface

```csharp
namespace Linksy.Sync.Connectors;

/// <summary>
/// Abstraction for external file platform connectors (ACC Docs, SharePoint).
/// Implementations must be thread-safe and support concurrent change operations.
/// </summary>
public interface IConnector
{
    /// <summary>
    /// Platform type identifier (ACC_DOCS or SHAREPOINT).
    /// </summary>
    ConnectorType ConnectorType { get; }
    
    /// <summary>
    /// Tenant ID for credential isolation and audit purposes.
    /// </summary>
    Guid TenantId { get; }
    
    /// <summary>
    /// Fetch incremental changes (deltas) from the platform since last checkpoint.
    /// </summary>
    /// <param name="deltaToken">Checkpoint token from prior sync; null = initial sync (all files)</param>
    /// <param name="cancellationToken">Cancellation support for long-running operations</param>
    /// <returns>List of detected changes (create, update, delete, move, rename) plus new delta token</returns>
    /// <remarks>
    /// - Both ACC Docs and SharePoint support delta queries via Microsoft Graph
    /// - Delta token should be persisted in BindingState after successful sync
    /// - Returns empty list if no changes since last delta token
    /// - Must include soft-deleted items (deletedItems collection)
    /// </remarks>
    Task<DeltaQueryResult> GetChangesAsync(string? deltaToken, CancellationToken cancellationToken);
    
    /// <summary>
    /// Upload or update a file on the target platform.
    /// </summary>
    /// <param name="metadata">File metadata (name, path, MIME type, size)</param>
    /// <param name="content">File content stream</param>
    /// <param name="overwrite">If true, replace existing file; if false, fail with conflict</param>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>Updated file metadata including version ID</returns>
    /// <remarks>
    /// - Automatically handles chunked uploads for files >100 MB (ACC Docs) or >60 MB (SharePoint)
    /// - Returns platform-specific version ID for conflict detection
    /// - Must validate checksum after upload (if platform supports) to ensure integrity
    /// - Throws ChangeConflictException if overwrite=false and file exists
    /// </remarks>
    Task<FileMetadata> UploadFileAsync(
        FileMetadata metadata,
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a file from the platform (soft delete to trash).
    /// </summary>
    /// <param name="fileId">Platform-specific file identifier</param>
    /// <param name="hardDelete">If true, permanently delete; if false, move to trash (recoverable)</param>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>True if deleted successfully; false if file not found</returns>
    /// <remarks>
    /// - Default behavior is soft delete (move to trash/recycle bin)
    /// - Hard delete should only be used for expiry-based purging
    /// - Both platforms support recovery from trash within 93 days (SharePoint) / unlimited (ACC)
    /// </remarks>
    Task<bool> DeleteFileAsync(
        string fileId,
        bool hardDelete = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that the connector has valid credentials and sufficient permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>Validation result with status and diagnostics</returns>
    /// <remarks>
    /// - Called during onboarding (FR-002) and credential rotation (FR-008a)
    /// - Should test read permissions on root folder + write permissions on temp sandbox folder
    /// - Automatically refreshes OAuth token if expired (401 Unauthorized)
    /// - Returns detailed error messages for troubleshooting (e.g., "Insufficient permissions: require Files.ReadWrite")
    /// </remarks>
    Task<CredentialValidation> ValidateCredentialsAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Get server time from the platform (for clock-skew detection in LastWriterWins conflicts).
    /// </summary>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>UTC timestamp according to platform server clock</returns>
    /// <remarks>
    /// - Used by LastWriterWins conflict policy to determine which version is newer
    /// - Protects against local clock skew (e.g., client clock off by 5 minutes)
    /// - Typically obtained from HTTP response header (Date) or API endpoint
    /// </remarks>
    Task<DateTime> GetServerTimeAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Download a specific file version from the platform.
    /// </summary>
    /// <param name="fileId">Platform-specific file identifier</param>
    /// <param name="versionId">Optional; specific version to download. If null, downloads latest.</param>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>Stream of file content for writing to target platform or local cache</returns>
    /// <remarks>
    /// - Used for conflict resolution (retrieve Source or Target version for comparison)
    /// - Used for quarantine restoration (re-download file version from archive)
    /// - Stream must be disposed by caller
    /// - Supports both ACC Docs versions and SharePoint version history
    /// </remarks>
    Task<Stream> DownloadFileAsync(
        string fileId,
        string? versionId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Platform type enumeration.
/// </summary>
public enum ConnectorType
{
    /// <summary>Autodesk Construction Cloud Docs (via Microsoft Graph)</summary>
    ACC_DOCS = 0,
    
    /// <summary>Microsoft SharePoint Online (via Microsoft Graph)</summary>
    SHAREPOINT = 1
}
```

---

## Data Models

### FileMetadata

```csharp
namespace Linksy.Sync.Models;

/// <summary>
/// File metadata for upload, download, and change detection.
/// </summary>
public class FileMetadata
{
    /// <summary>Platform-specific file identifier (immutable).</summary>
    public string FileId { get; set; } = string.Empty;
    
    /// <summary>File name only (e.g., "design-v1.dwg").</summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>Full folder path + filename (e.g., "/Projects/Building-A/design-v1.dwg").</summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>MIME type of file (e.g., "application/vnd.autodesk", "application/vnd.openxmlformats-officedocument.wordprocessingml.document").</summary>
    public string MimeType { get; set; } = string.Empty;
    
    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }
    
    /// <summary>Platform-specific version identifier for change tracking and conflict detection.</summary>
    public string VersionId { get; set; } = string.Empty;
    
    /// <summary>User ID or email address of the user who last modified the file.</summary>
    public string ModifiedBy { get; set; } = string.Empty;
    
    /// <summary>UTC timestamp of last modification on the source platform (for clock-skew detection).</summary>
    public DateTime ModifiedAtUtc { get; set; }
    
    /// <summary>SHA256 checksum of file content (hex-encoded). Used for integrity verification.</summary>
    public string? ContentChecksum { get; set; }
    
    /// <summary>Custom application metadata (e.g., Revit project ID, CAD layer info). Stored as JSON.</summary>
    public Dictionary<string, object>? CustomMetadata { get; set; }
}
```

### ChangeItem

```csharp
namespace Linksy.Sync.Models;

/// <summary>
/// Represents a detected change (create, update, delete, move, rename) to be synced to the target platform.
/// </summary>
public class ChangeItem
{
    /// <summary>Unique identifier for this change within a sync job.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Sync job containing this change.</summary>
    public Guid SyncJobId { get; set; }
    
    /// <summary>Binding associated with this change.</summary>
    public Guid BindingId { get; set; }
    
    /// <summary>Platform-specific file identifier.</summary>
    public string FileId { get; set; } = string.Empty;
    
    /// <summary>Full file path (e.g., "/Projects/Building-A/design-v1.dwg").</summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>File name only.</summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>Type of change detected.</summary>
    public ChangeAction Action { get; set; }
    
    /// <summary>Platform where the change originated (ACC_DOCS or SHAREPOINT).</summary>
    public ConnectorType SourcePlatform { get; set; }
    
    /// <summary>Platform where the change should be applied (opposite of source).</summary>
    public ConnectorType TargetPlatform { get; set; }
    
    /// <summary>Source platform file metadata before the change.</summary>
    public FileMetadata? SourceMetadata { get; set; }
    
    /// <summary>Target platform file metadata (if exists) for conflict detection.</summary>
    public FileMetadata? TargetMetadata { get; set; }
    
    /// <summary>Current processing status.</summary>
    public ChangeItemStatus Status { get; set; }
    
    /// <summary>Number of retry attempts made for this change (due to transient errors or throttling).</summary>
    public int RetryCount { get; set; }
    
    /// <summary>If Status == Conflict, which conflict policy was applied.</summary>
    public ConflictPolicy? ConflictPolicyApplied { get; set; }
    
    /// <summary>If Status == Quarantined, timestamp after which item should be purged (7 days from creation).</summary>
    public DateTime? QuarantineExpiresAtUtc { get; set; }
    
    /// <summary>Human-readable error message if Status == Failed or ManualHold.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Type of file change.
/// </summary>
public enum ChangeAction
{
    /// <summary>File was created on source platform.</summary>
    Create = 0,
    
    /// <summary>File was modified (content or metadata) on source platform.</summary>
    Update = 1,
    
    /// <summary>File was deleted (soft delete) on source platform.</summary>
    Delete = 2,
    
    /// <summary>File was moved to a different folder on source platform.</summary>
    Move = 3,
    
    /// <summary>File was renamed on source platform.</summary>
    Rename = 4
}

/// <summary>
/// Processing status of a change item.
/// </summary>
public enum ChangeItemStatus
{
    /// <summary>Change detected but not yet processed.</summary>
    Pending = 0,
    
    /// <summary>Change is currently being applied to target platform.</summary>
    InProgress = 1,
    
    /// <summary>Change successfully applied to target platform.</summary>
    Completed = 2,
    
    /// <summary>Change encountered a permanent error (e.g., permission denied).</summary>
    Failed = 3,
    
    /// <summary>Change is conflicting with target platform state; awaiting operator decision.</summary>
    ManualHold = 4,
    
    /// <summary>Change was soft-deleted and moved to quarantine (7-day recovery window).</summary>
    Quarantined = 5
}

/// <summary>
/// Conflict resolution policies for when source and target diverge.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>Source platform version always wins; overwrite target.</summary>
    SourceWins = 0,
    
    /// <summary>Target platform version always wins; discard source change.</summary>
    TargetWins = 1,
    
    /// <summary>Most recently modified version wins (uses platform server timestamps to prevent clock-skew issues).</summary>
    LastWriterWins = 2,
    
    /// <summary>Flag conflict for operator to manually resolve in the portal.</summary>
    ManualHold = 3
}
```

### DeltaQueryResult

```csharp
namespace Linksy.Sync.Models;

/// <summary>
/// Result of a delta query (GetChangesAsync), including detected changes and continuation token.
/// </summary>
public class DeltaQueryResult
{
    /// <summary>List of detected changes since the prior delta token.</summary>
    public IEnumerable<ChangeItem> Changes { get; set; } = new List<ChangeItem>();
    
    /// <summary>
    /// New delta token to persist in BindingState for the next sync cycle.
    /// If null, indicates no further changes to query (end of current batch).
    /// </summary>
    public string? NextDeltaToken { get; set; }
    
    /// <summary>
    /// Indicates whether more changes exist beyond this batch (pagination support).
    /// If true, caller should fetch again with NextDeltaToken to retrieve additional changes.
    /// </summary>
    public bool HasMoreChanges { get; set; }
    
    /// <summary>
    /// Total number of changes detected in this query result.
    /// </summary>
    public int TotalChangesCount { get; set; }
}
```

### CredentialValidation

```csharp
namespace Linksy.Sync.Models;

/// <summary>
/// Result of credential validation (used during onboarding and credential rotation).
/// </summary>
public class CredentialValidation
{
    /// <summary>Overall validation status.</summary>
    public ValidationStatus Status { get; set; }
    
    /// <summary>Human-readable message explaining validation outcome.</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>True if the connector can read files from the configured folder.</summary>
    public bool CanRead { get; set; }
    
    /// <summary>True if the connector can write (create/update/delete) files in the configured folder.</summary>
    public bool CanWrite { get; set; }
    
    /// <summary>True if the connector can access the configured folder (fundamental permission check).</summary>
    public bool HasAccessToFolder { get; set; }
    
    /// <summary>Diagnostic messages for troubleshooting (e.g., "Missing Files.ReadWrite permission", "Folder not found").</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
    
    /// <summary>UTC timestamp when this validation was performed.</summary>
    public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation result status.
/// </summary>
public enum ValidationStatus
{
    /// <summary>All checks passed; connector is ready for sync.</summary>
    Success = 0,
    
    /// <summary>Validation is in progress (used for async checks like permission testing).</summary>
    InProgress = 1,
    
    /// <summary>One or more checks failed; see Diagnostics for details.</summary>
    Failed = 2,
    
    /// <summary>Validation was skipped (e.g., timeout, network issue); recommend retry.</summary>
    SkippedRetryLater = 3
}
```

---

## Connector Implementations

### AccDocsConnector

**Namespace**: `Linksy.Sync.Connectors`

**Dependencies**:
- Microsoft.Graph (v5.95.0+)
- Azure.Identity (for DefaultAzureCredential or ManagedIdentityCredential)

**Configuration**:
```json
{
  "ConnectorSettings:AccDocs:TenantId": "your-azure-ad-tenant-id",
  "ConnectorSettings:AccDocs:ClientId": "your-app-registration-client-id",
  "ConnectorSettings:AccDocs:ClientSecret": "from-azure-keyvault",
  "ConnectorSettings:AccDocs:Scopes": ["https://graph.microsoft.com/.default"]
}
```

**Key Details**:
- Uses Microsoft Graph Drive API: `/me/drive/items/delta`
- Tracks changes with `delta` query parameter
- Supports soft-delete tracking via `track=true` query parameter
- File upload: Resumable session uploads for >100 MB (PUT /content)
- Rate limits: ~1000 requests/minute per app
- Deleted items recovery: 93-day retention in trash

---

### SharePointConnector

**Namespace**: `Linksy.Sync.Connectors`

**Dependencies**:
- Microsoft.Graph (v5.95.0+)
- Azure.Identity

**Configuration**:
```json
{
  "ConnectorSettings:SharePoint:TenantId": "your-sharepoint-tenant-id",
  "ConnectorSettings:SharePoint:SiteId": "site-collection-id",
  "ConnectorSettings:SharePoint:ClientId": "your-app-registration-client-id",
  "ConnectorSettings:SharePoint:ClientSecret": "from-azure-keyvault"
}
```

**Key Details**:
- Uses Microsoft Graph SharePoint Drive API: `/sites/{siteId}/drive/items/delta`
- Same delta query pattern as ACC Docs
- Supports version history queries for conflict resolution
- File upload: Resumable session uploads for >60 MB
- Rate limits: ~1000–2000 requests/minute per app
- Deleted items recovery: 93-day retention in recycle bin
- Version control: Each change includes versionId for tracking

---

## Error Handling

All connector methods must throw appropriate exceptions:

```csharp
namespace Linksy.Sync.Connectors;

/// <summary>
/// Base exception for connector-related errors.
/// </summary>
public class ConnectorException : Exception
{
    public ConnectorException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when file already exists on target platform and overwrite=false.
/// </summary>
public class ChangeConflictException : ConnectorException
{
    public ChangeConflictException(string fileId, string targetVersion)
        : base($"File {fileId} exists on target (version {targetVersion}); set overwrite=true to replace") { }
}

/// <summary>
/// Thrown when connector lacks required permissions.
/// </summary>
public class InsufficientPermissionsException : ConnectorException
{
    public InsufficientPermissionsException(string requiredPermission)
        : base($"Connector lacks required permission: {requiredPermission}") { }
}

/// <summary>
/// Thrown when external platform is temporarily unavailable (rate-limited, service outage).
/// Intended to be retried with exponential backoff.
/// </summary>
public class TemporaryException : ConnectorException
{
    public TemporaryException(string message, int? retryAfterSeconds = null)
        : base($"{message} (retry after {retryAfterSeconds ?? 60}s)") { }
}

/// <summary>
/// Thrown when credentials are no longer valid (expired OAuth token, user revoked access).
/// Requires credential refresh or re-authentication.
/// </summary>
public class InvalidCredentialsException : ConnectorException
{
    public InvalidCredentialsException(string message)
        : base($"Credentials invalid: {message}") { }
}
```

---

## Retry & Resilience Strategy

All connectors must implement the following resilience strategy (see `research.md` Task 5 for details):

1. **Transient Errors** (TemporaryException, TimeoutException, HttpRequestException):
   - Exponential backoff with jitter: 1s, 2s, 4s, 8s, 16s, 30s, 40s (7 retries max)
   - Total elapsed time: ≥120 seconds (spans 2+ rate-limit cycles)
   - Example: If 429 returned on attempt 1, wait 1.2s; retry. If 429 returned on attempt 2, wait 2.4s; retry.

2. **Permanent Errors** (InvalidCredentialsException, InsufficientPermissionsException, FileNotFoundException):
   - No retry; escalate to ManualHold queue
   - Alert operator via observability metrics

3. **Rate-Limiting** (HTTP 429 Throttling):
   - Respect `Retry-After` header if provided
   - Fall back to exponential backoff if not
   - Emit `sync_throttle_events_total` metric for observability

---

## Testing

### Mock Connector for Testing

```csharp
namespace Linksy.Sync.Tests;

public class MockConnector : IConnector
{
    public ConnectorType ConnectorType => ConnectorType.ACC_DOCS;
    public Guid TenantId { get; }
    
    private Dictionary<string, FileMetadata> _files = new();
    
    public MockConnector(Guid tenantId)
    {
        TenantId = tenantId;
    }
    
    public Task<DeltaQueryResult> GetChangesAsync(string? deltaToken, CancellationToken cancellationToken)
    {
        // Return pre-configured test changes
        return Task.FromResult(new DeltaQueryResult
        {
            Changes = _files.Values.Select(f => new ChangeItem
            {
                FileId = f.FileId,
                FilePath = f.FilePath,
                FileName = f.FileName,
                Action = ChangeAction.Create,
                SourcePlatform = ConnectorType.ACC_DOCS,
                TargetPlatform = ConnectorType.SHAREPOINT,
                SourceMetadata = f,
                Status = ChangeItemStatus.Pending
            }),
            NextDeltaToken = "mock-delta-token",
            HasMoreChanges = false,
            TotalChangesCount = _files.Count
        });
    }
    
    public Task<FileMetadata> UploadFileAsync(FileMetadata metadata, Stream content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (_files.ContainsKey(metadata.FileId) && !overwrite)
            throw new ChangeConflictException(metadata.FileId, _files[metadata.FileId].VersionId);
        
        _files[metadata.FileId] = metadata;
        return Task.FromResult(metadata);
    }
    
    public Task<bool> DeleteFileAsync(string fileId, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_files.Remove(fileId));
    }
    
    public Task<CredentialValidation> ValidateCredentialsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new CredentialValidation
        {
            Status = ValidationStatus.Success,
            Message = "Mock credentials valid",
            CanRead = true,
            CanWrite = true,
            HasAccessToFolder = true,
            ValidatedAtUtc = DateTime.UtcNow
        });
    }
    
    public Task<DateTime> GetServerTimeAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(DateTime.UtcNow);
    }
    
    public Task<Stream> DownloadFileAsync(string fileId, string? versionId = null, CancellationToken cancellationToken = default)
    {
        if (!_files.ContainsKey(fileId))
            throw new FileNotFoundException($"File {fileId} not found");
        
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        return Task.FromResult<Stream>(content);
    }
}
```

### Integration Test Example

```csharp
[Fact]
public async Task GetChangesAsync_WithValidDeltaToken_ReturnsIncrementalChanges()
{
    // Arrange
    var connector = new AccDocsConnector(tenantId, graphClient, logger);
    var deltaToken = "prev-delta-token";
    
    // Act
    var result = await connector.GetChangesAsync(deltaToken, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result.NextDeltaToken);
    Assert.NotEmpty(result.Changes);
    Assert.Contains(result.Changes, c => c.Action == ChangeAction.Create);
}

[Fact]
public async Task ValidateCredentialsAsync_WithInvalidToken_ThrowsInvalidCredentialsException()
{
    // Arrange
    var connector = new AccDocsConnector(tenantId, invalidGraphClient, logger);
    
    // Act & Assert
    await Assert.ThrowsAsync<InvalidCredentialsException>(
        () => connector.ValidateCredentialsAsync(CancellationToken.None)
    );
}
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-10-26 | Initial connector interface specification; covers ACC Docs and SharePoint implementations |

