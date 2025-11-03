namespace Linksy.Api.Models;

/// <summary>
/// Tracks execution instance for a binding with status, processed changes, and telemetry
/// </summary>
public class SyncJob
{
    public Guid Id { get; set; }
    public Guid BindingId { get; set; }
    public SyncJobStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int FilesProcessed { get; set; }
    public long BytesTransferred { get; set; }
    public int ConflictsDetected { get; set; }
    public int Retries { get; set; }

    public string? ErrorMessage { get; set; }
    public string? TelemetryReference { get; set; }
    public bool IsManualTrigger { get; set; }

    // Navigation properties
    public Binding Binding { get; set; } = null!;
    public ICollection<ChangeItem> ChangeItems { get; set; } = [];
}
