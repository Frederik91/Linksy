namespace Linksy.Api.Models;

/// <summary>
/// Types of audited actions in the system
/// </summary>
public enum AuditActionType
{
    TenantCreated,
    TenantUpdated,
    ConnectorCreated,
    ConnectorUpdated,
    ConnectorDeleted,
    BindingCreated,
    BindingUpdated,
    BindingPaused,
    BindingResumed,
    BindingDeleted,
    SyncJobStarted,
    SyncJobCompleted,
    SyncJobFailed,
    FileCreated,
    FileUpdated,
    FileDeleted,
    FileMoved,
    FileRenamed,
    ConflictResolved,
    CredentialRotated
}
