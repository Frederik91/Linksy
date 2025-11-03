namespace Linksy.Api.Models;

/// <summary>
/// Defines a pair of folders, sync direction, conflict policy, and schedule cadence
/// </summary>
public class Binding
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceConnectorId { get; set; }
    public Guid TargetConnectorId { get; set; }

    public required string SourcePath { get; set; }
    public required string TargetPath { get; set; }

    public SyncDirection Direction { get; set; }
    public ConflictPolicy ConflictPolicy { get; set; }

    public string? Filters { get; set; } // JSON filter configuration
    public int ScheduleCadenceMinutes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }

    public string? SourceDeltaToken { get; set; }
    public string? TargetDeltaToken { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Connector SourceConnector { get; set; } = null!;
    public Connector TargetConnector { get; set; } = null!;
    public ICollection<SyncJob> SyncJobs { get; set; } = [];
}
