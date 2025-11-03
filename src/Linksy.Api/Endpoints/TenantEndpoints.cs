using Linksy.Api.Data;
using Linksy.Api.DTOs;
using Linksy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Linksy.Api.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants")
            .WithOpenApi();

        // Get all tenants
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var tenants = await db.Tenants
                .Select(t => new TenantDto(
                    t.Id,
                    t.Name,
                    t.BillingProfile,
                    t.CreatedAt,
                    t.IsActive
                ))
                .ToListAsync();

            return Results.Ok(tenants);
        })
        .WithName("GetAllTenants")
        .Produces<List<TenantDto>>(StatusCodes.Status200OK);

        // Get tenant by ID
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);

            if (tenant is null)
                return Results.NotFound(new { message = "Tenant not found" });

            var tenantDto = new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.BillingProfile,
                tenant.CreatedAt,
                tenant.IsActive
            );

            return Results.Ok(tenantDto);
        })
        .WithName("GetTenantById")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Create tenant
        group.MapPost("/", async (CreateTenantRequest request, ApplicationDbContext db) =>
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                BillingProfile = request.BillingProfile,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var tenantDto = new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.BillingProfile,
                tenant.CreatedAt,
                tenant.IsActive
            );

            return Results.Created($"/api/tenants/{tenant.Id}", tenantDto);
        })
        .WithName("CreateTenant")
        .Produces<TenantDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Update tenant
        group.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest request, ApplicationDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);

            if (tenant is null)
                return Results.NotFound(new { message = "Tenant not found" });

            tenant.Name = request.Name;
            tenant.BillingProfile = request.BillingProfile;
            tenant.IsActive = request.IsActive;
            tenant.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var tenantDto = new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.BillingProfile,
                tenant.CreatedAt,
                tenant.IsActive
            );

            return Results.Ok(tenantDto);
        })
        .WithName("UpdateTenant")
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Delete tenant
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);

            if (tenant is null)
                return Results.NotFound(new { message = "Tenant not found" });

            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteTenant")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
