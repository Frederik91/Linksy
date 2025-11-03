using Linksy.Api.Models;

namespace Linksy.Api.DTOs;

public record ConnectorDto(
    Guid Id,
    Guid TenantId,
    ConnectorType Type,
    string Name,
    DateTime CreatedAt,
    bool IsHealthy,
    string? HealthMessage,
    DateTime? LastHealthCheck
);

public record CreateConnectorRequest(
    Guid TenantId,
    ConnectorType Type,
    string Name,
    string Configuration,
    string EncryptedCredential,
    string KeyIdentifier
);

public record UpdateConnectorRequest(
    string Name,
    string? Configuration
);

public record ConnectorHealthDto(
    bool IsHealthy,
    string Message,
    DateTime CheckedAt
);
