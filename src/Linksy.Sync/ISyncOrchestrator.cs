namespace Linksy.Sync;

/// <summary>
/// Sync orchestrator for coordinating file synchronization between ACC and SharePoint.
/// Uses Azure Durable Functions to manage workflow orchestration and state.
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Starts a new sync job for the given workspace and project.
    /// </summary>
    Task<string> StartSyncJobAsync(string workspaceId, string projectId);

    /// <summary>
    /// Gets the status of a sync job.
    /// </summary>
    Task<SyncJobStatus> GetSyncJobStatusAsync(string jobId);

    /// <summary>
    /// Cancels a running sync job.
    /// </summary>
    Task CancelSyncJobAsync(string jobId);
}

/// <summary>
/// Represents the status of a sync job.
/// </summary>
public class SyncJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int FilesProcessed { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorMessage { get; set; }
}
