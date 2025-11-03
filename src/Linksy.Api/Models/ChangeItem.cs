namespace Linksy.Api.Models;

/// <summary>
/// Represents an individual file or folder delta with source metadata and target action
/// </summary>
public class ChangeItem
{
    public Guid Id { get; set; }
    public Guid SyncJobId { get; set; }

    public required string SourcePath { get; set; }
    public required string TargetPath { get; set; }

    public ChangeAction Action { get; set; }
    public ChangeItemStatus Status { get; set; }

    public string? Checksum { get; set; }
    public long? SizeBytes { get; set; }
    public string? VersionId { get; set; }
    public string? SourceMetadata { get; set; } // JSON metadata

    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }
    public bool IsConflict { get; set; }

    // Navigation properties
    public SyncJob SyncJob { get; set; } = null!;
}
