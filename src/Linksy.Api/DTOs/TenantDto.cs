namespace Linksy.Api.DTOs;

public record TenantDto(
    Guid Id,
    string Name,
    string? BillingProfile,
    DateTime CreatedAt,
    bool IsActive
);

public record CreateTenantRequest(
    string Name,
    string? BillingProfile
);

public record UpdateTenantRequest(
    string Name,
    string? BillingProfile,
    bool IsActive
);
