namespace Linksy.Api.Models;

/// <summary>
/// Status of a sync job execution
/// </summary>
public enum SyncJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    PartialSuccess
}
