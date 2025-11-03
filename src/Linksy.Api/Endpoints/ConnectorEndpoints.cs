using Linksy.Api.Data;
using Linksy.Api.DTOs;
using Linksy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Linksy.Api.Endpoints;

public static class ConnectorEndpoints
{
    public static void MapConnectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/connectors")
            .WithTags("Connectors")
            .WithOpenApi();

        // Get all connectors for a tenant
        group.MapGet("/tenant/{tenantId:guid}", async (Guid tenantId, ApplicationDbContext db) =>
        {
            var connectors = await db.Connectors
                .Where(c => c.TenantId == tenantId)
                .Select(c => new ConnectorDto(
                    c.Id,
                    c.TenantId,
                    c.Type,
                    c.Name,
                    c.CreatedAt,
                    c.IsHealthy,
                    c.HealthMessage,
                    c.LastHealthCheck
                ))
                .ToListAsync();

            return Results.Ok(connectors);
        })
        .WithName("GetConnectorsByTenant")
        .Produces<List<ConnectorDto>>(StatusCodes.Status200OK);

        // Get connector by ID
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var connector = await db.Connectors.FindAsync(id);

            if (connector is null)
                return Results.NotFound(new { message = "Connector not found" });

            var connectorDto = new ConnectorDto(
                connector.Id,
                connector.TenantId,
                connector.Type,
                connector.Name,
                connector.CreatedAt,
                connector.IsHealthy,
                connector.HealthMessage,
                connector.LastHealthCheck
            );

            return Results.Ok(connectorDto);
        })
        .WithName("GetConnectorById")
        .Produces<ConnectorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Create connector
        group.MapPost("/", async (CreateConnectorRequest request, ApplicationDbContext db) =>
        {
            // Verify tenant exists
            var tenantExists = await db.Tenants.AnyAsync(t => t.Id == request.TenantId);
            if (!tenantExists)
                return Results.BadRequest(new { message = "Tenant not found" });

            var connector = new Connector
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Type = request.Type,
                Name = request.Name,
                Configuration = request.Configuration,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsHealthy = false
            };

            var credentialSecret = new CredentialSecret
            {
                Id = Guid.NewGuid(),
                ConnectorId = connector.Id,
                EncryptedCredential = request.EncryptedCredential,
                KeyIdentifier = request.KeyIdentifier,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RequiresRotation = false
            };

            db.Connectors.Add(connector);
            db.CredentialSecrets.Add(credentialSecret);
            await db.SaveChangesAsync();

            var connectorDto = new ConnectorDto(
                connector.Id,
                connector.TenantId,
                connector.Type,
                connector.Name,
                connector.CreatedAt,
                connector.IsHealthy,
                connector.HealthMessage,
                connector.LastHealthCheck
            );

            return Results.Created($"/api/connectors/{connector.Id}", connectorDto);
        })
        .WithName("CreateConnector")
        .Produces<ConnectorDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Update connector
        group.MapPut("/{id:guid}", async (Guid id, UpdateConnectorRequest request, ApplicationDbContext db) =>
        {
            var connector = await db.Connectors.FindAsync(id);

            if (connector is null)
                return Results.NotFound(new { message = "Connector not found" });

            connector.Name = request.Name;
            if (request.Configuration is not null)
                connector.Configuration = request.Configuration;
            connector.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var connectorDto = new ConnectorDto(
                connector.Id,
                connector.TenantId,
                connector.Type,
                connector.Name,
                connector.CreatedAt,
                connector.IsHealthy,
                connector.HealthMessage,
                connector.LastHealthCheck
            );

            return Results.Ok(connectorDto);
        })
        .WithName("UpdateConnector")
        .Produces<ConnectorDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Check connector health
        group.MapPost("/{id:guid}/health-check", async (Guid id, ApplicationDbContext db) =>
        {
            var connector = await db.Connectors.FindAsync(id);

            if (connector is null)
                return Results.NotFound(new { message = "Connector not found" });

            // TODO: Implement actual health check logic for ACC Docs and SharePoint
            connector.IsHealthy = true;
            connector.HealthMessage = "Health check passed";
            connector.LastHealthCheck = DateTime.UtcNow;
            connector.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var healthDto = new ConnectorHealthDto(
                connector.IsHealthy,
                connector.HealthMessage ?? "OK",
                connector.LastHealthCheck.Value
            );

            return Results.Ok(healthDto);
        })
        .WithName("CheckConnectorHealth")
        .Produces<ConnectorHealthDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Delete connector
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var connector = await db.Connectors.FindAsync(id);

            if (connector is null)
                return Results.NotFound(new { message = "Connector not found" });

            db.Connectors.Remove(connector);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteConnector")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
