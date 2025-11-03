namespace Linksy.Api.Models;

/// <summary>
/// Describes an external platform integration instance (e.g., ACC Docs, SharePoint)
/// </summary>
public class Connector
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public ConnectorType Type { get; set; }
    public required string Name { get; set; }
    public required string Configuration { get; set; } // JSON configuration
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public bool IsHealthy { get; set; }
    public string? HealthMessage { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public CredentialSecret? CredentialSecret { get; set; }
    public ICollection<Binding> SourceBindings { get; set; } = [];
    public ICollection<Binding> TargetBindings { get; set; } = [];
}
