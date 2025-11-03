using Linksy.Api.Models;

namespace Linksy.Api.DTOs;

public record AuditEntryDto(
    Guid Id,
    Guid TenantId,
    AuditActionType ActionType,
    string Actor,
    DateTime Timestamp,
    Guid? BindingId,
    Guid? SyncJobId,
    string Outcome
);

public record AuditQueryRequest(
    Guid TenantId,
    DateTime? StartDate,
    DateTime? EndDate,
    AuditActionType? ActionType,
    int PageNumber = 1,
    int PageSize = 50
);
