using Linksy.Api.Models;

namespace Linksy.Api.DTOs;

public record SyncJobDto(
    Guid Id,
    Guid BindingId,
    SyncJobStatus Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int FilesProcessed,
    long BytesTransferred,
    int ConflictsDetected,
    int Retries,
    string? ErrorMessage,
    bool IsManualTrigger
);

public record CreateSyncJobRequest(
    Guid BindingId,
    bool IsManualTrigger
);

public record SyncJobStatsDto(
    int TotalJobs,
    int CompletedJobs,
    int FailedJobs,
    int RunningJobs,
    long TotalBytesTransferred,
    int TotalFilesProcessed
);
