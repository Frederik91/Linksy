using Linksy.Api.Data;
using Linksy.Api.DTOs;
using Linksy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Linksy.Api.Endpoints;

public static class BindingEndpoints
{
    public static void MapBindingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bindings")
            .WithTags("Bindings")
            .WithOpenApi();

        // Get all bindings for a tenant
        group.MapGet("/tenant/{tenantId:guid}", async (Guid tenantId, ApplicationDbContext db) =>
        {
            var bindings = await db.Bindings
                .Where(b => b.TenantId == tenantId)
                .Select(b => new BindingDto(
                    b.Id,
                    b.TenantId,
                    b.SourceConnectorId,
                    b.TargetConnectorId,
                    b.SourcePath,
                    b.TargetPath,
                    b.Direction,
                    b.ConflictPolicy,
                    b.ScheduleCadenceMinutes,
                    b.IsActive,
                    b.IsPaused,
                    b.CreatedAt
                ))
                .ToListAsync();

            return Results.Ok(bindings);
        })
        .WithName("GetBindingsByTenant")
        .Produces<List<BindingDto>>(StatusCodes.Status200OK);

        // Get binding by ID
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var binding = await db.Bindings.FindAsync(id);

            if (binding is null)
                return Results.NotFound(new { message = "Binding not found" });

            var bindingDto = new BindingDto(
                binding.Id,
                binding.TenantId,
                binding.SourceConnectorId,
                binding.TargetConnectorId,
                binding.SourcePath,
                binding.TargetPath,
                binding.Direction,
                binding.ConflictPolicy,
                binding.ScheduleCadenceMinutes,
                binding.IsActive,
                binding.IsPaused,
                binding.CreatedAt
            );

            return Results.Ok(bindingDto);
        })
        .WithName("GetBindingById")
        .Produces<BindingDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Create binding
        group.MapPost("/", async (CreateBindingRequest request, ApplicationDbContext db) =>
        {
            // Validate tenant and connectors exist
            var tenantExists = await db.Tenants.AnyAsync(t => t.Id == request.TenantId);
            if (!tenantExists)
                return Results.BadRequest(new { message = "Tenant not found" });

            var sourceExists = await db.Connectors.AnyAsync(c => c.Id == request.SourceConnectorId);
            var targetExists = await db.Connectors.AnyAsync(c => c.Id == request.TargetConnectorId);

            if (!sourceExists || !targetExists)
                return Results.BadRequest(new { message = "One or more connectors not found" });

            var binding = new Binding
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                SourceConnectorId = request.SourceConnectorId,
                TargetConnectorId = request.TargetConnectorId,
                SourcePath = request.SourcePath,
                TargetPath = request.TargetPath,
                Direction = request.Direction,
                ConflictPolicy = request.ConflictPolicy,
                ScheduleCadenceMinutes = request.ScheduleCadenceMinutes,
                Filters = request.Filters,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsPaused = false
            };

            db.Bindings.Add(binding);
            await db.SaveChangesAsync();

            var bindingDto = new BindingDto(
                binding.Id,
                binding.TenantId,
                binding.SourceConnectorId,
                binding.TargetConnectorId,
                binding.SourcePath,
                binding.TargetPath,
                binding.Direction,
                binding.ConflictPolicy,
                binding.ScheduleCadenceMinutes,
                binding.IsActive,
                binding.IsPaused,
                binding.CreatedAt
            );

            return Results.Created($"/api/bindings/{binding.Id}", bindingDto);
        })
        .WithName("CreateBinding")
        .Produces<BindingDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Update binding
        group.MapPut("/{id:guid}", async (Guid id, UpdateBindingRequest request, ApplicationDbContext db) =>
        {
            var binding = await db.Bindings.FindAsync(id);

            if (binding is null)
                return Results.NotFound(new { message = "Binding not found" });

            if (request.SourcePath is not null) binding.SourcePath = request.SourcePath;
            if (request.TargetPath is not null) binding.TargetPath = request.TargetPath;
            if (request.Direction.HasValue) binding.Direction = request.Direction.Value;
            if (request.ConflictPolicy.HasValue) binding.ConflictPolicy = request.ConflictPolicy.Value;
            if (request.ScheduleCadenceMinutes.HasValue) binding.ScheduleCadenceMinutes = request.ScheduleCadenceMinutes.Value;
            if (request.Filters is not null) binding.Filters = request.Filters;
            if (request.IsActive.HasValue) binding.IsActive = request.IsActive.Value;
            if (request.IsPaused.HasValue) binding.IsPaused = request.IsPaused.Value;

            binding.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var bindingDto = new BindingDto(
                binding.Id,
                binding.TenantId,
                binding.SourceConnectorId,
                binding.TargetConnectorId,
                binding.SourcePath,
                binding.TargetPath,
                binding.Direction,
                binding.ConflictPolicy,
                binding.ScheduleCadenceMinutes,
                binding.IsActive,
                binding.IsPaused,
                binding.CreatedAt
            );

            return Results.Ok(bindingDto);
        })
        .WithName("UpdateBinding")
        .Produces<BindingDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Pause binding
        group.MapPost("/{id:guid}/pause", async (Guid id, ApplicationDbContext db) =>
        {
            var binding = await db.Bindings.FindAsync(id);

            if (binding is null)
                return Results.NotFound(new { message = "Binding not found" });

            binding.IsPaused = true;
            binding.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Binding paused successfully" });
        })
        .WithName("PauseBinding")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Resume binding
        group.MapPost("/{id:guid}/resume", async (Guid id, ApplicationDbContext db) =>
        {
            var binding = await db.Bindings.FindAsync(id);

            if (binding is null)
                return Results.NotFound(new { message = "Binding not found" });

            binding.IsPaused = false;
            binding.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Binding resumed successfully" });
        })
        .WithName("ResumeBinding")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Delete binding
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var binding = await db.Bindings.FindAsync(id);

            if (binding is null)
                return Results.NotFound(new { message = "Binding not found" });

            db.Bindings.Remove(binding);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteBinding")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
