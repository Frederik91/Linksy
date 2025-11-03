namespace Linksy.Api.Models;

/// <summary>
/// Encapsulates encrypted app-only credential material linked to a connector
/// </summary>
public class CredentialSecret
{
    public Guid Id { get; set; }
    public Guid ConnectorId { get; set; }

    public required string EncryptedCredential { get; set; }
    public required string KeyIdentifier { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastRotatedAt { get; set; }

    public bool RequiresRotation { get; set; }
    public string? RotationHistory { get; set; } // JSON history

    // Navigation properties
    public Connector Connector { get; set; } = null!;
}
