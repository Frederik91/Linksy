using Linksy.Api.Models;

namespace Linksy.Api.DTOs;

public record BindingDto(
    Guid Id,
    Guid TenantId,
    Guid SourceConnectorId,
    Guid TargetConnectorId,
    string SourcePath,
    string TargetPath,
    SyncDirection Direction,
    ConflictPolicy ConflictPolicy,
    int ScheduleCadenceMinutes,
    bool IsActive,
    bool IsPaused,
    DateTime CreatedAt
);

public record CreateBindingRequest(
    Guid TenantId,
    Guid SourceConnectorId,
    Guid TargetConnectorId,
    string SourcePath,
    string TargetPath,
    SyncDirection Direction,
    ConflictPolicy ConflictPolicy,
    int ScheduleCadenceMinutes,
    string? Filters
);

public record UpdateBindingRequest(
    string? SourcePath,
    string? TargetPath,
    SyncDirection? Direction,
    ConflictPolicy? ConflictPolicy,
    int? ScheduleCadenceMinutes,
    string? Filters,
    bool? IsActive,
    bool? IsPaused
);
