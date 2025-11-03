namespace Linksy.Api.Models;

/// <summary>
/// Immutable log record capturing user or system action, context, and outcome
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public AuditActionType ActionType { get; set; }
    public required string Actor { get; set; } // User ID or "System"

    public DateTime Timestamp { get; set; }

    public Guid? BindingId { get; set; }
    public Guid? SyncJobId { get; set; }

    public required string Context { get; set; } // JSON context
    public required string Outcome { get; set; } // Success/Failure/PartialSuccess

    public string? AdditionalData { get; set; } // JSON additional data

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
