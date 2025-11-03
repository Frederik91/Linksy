namespace Linksy.Api.Models;

/// <summary>
/// Represents a customer organization with portal users, access policies, and connector consents
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? BillingProfile { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Connector> Connectors { get; set; } = [];
    public ICollection<Binding> Bindings { get; set; } = [];
    public ICollection<AuditEntry> AuditEntries { get; set; } = [];
}
