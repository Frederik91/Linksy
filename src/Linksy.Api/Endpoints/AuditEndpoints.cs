using Linksy.Api.Data;
using Linksy.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Linksy.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit")
            .WithOpenApi();

        // Query audit entries
        group.MapPost("/query", async (AuditQueryRequest request, ApplicationDbContext db) =>
        {
            var query = db.AuditEntries
                .Where(a => a.TenantId == request.TenantId);

            if (request.StartDate.HasValue)
                query = query.Where(a => a.Timestamp >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(a => a.Timestamp <= request.EndDate.Value);

            if (request.ActionType.HasValue)
                query = query.Where(a => a.ActionType == request.ActionType.Value);

            var totalCount = await query.CountAsync();

            var entries = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new AuditEntryDto(
                    a.Id,
                    a.TenantId,
                    a.ActionType,
                    a.Actor,
                    a.Timestamp,
                    a.BindingId,
                    a.SyncJobId,
                    a.Outcome
                ))
                .ToListAsync();

            return Results.Ok(new
            {
                entries,
                totalCount,
                pageNumber = request.PageNumber,
                pageSize = request.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            });
        })
        .WithName("QueryAuditEntries")
        .Produces(StatusCodes.Status200OK);

        // Get audit entry by ID
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var entry = await db.AuditEntries.FindAsync(id);

            if (entry is null)
                return Results.NotFound(new { message = "Audit entry not found" });

            var entryDto = new AuditEntryDto(
                entry.Id,
                entry.TenantId,
                entry.ActionType,
                entry.Actor,
                entry.Timestamp,
                entry.BindingId,
                entry.SyncJobId,
                entry.Outcome
            );

            return Results.Ok(entryDto);
        })
        .WithName("GetAuditEntryById")
        .Produces<AuditEntryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Export audit log (placeholder for future implementation)
        group.MapPost("/export", async (AuditQueryRequest request, ApplicationDbContext db) =>
        {
            // TODO: Implement export to CSV/JSON
            return Results.Ok(new { message = "Export functionality coming soon" });
        })
        .WithName("ExportAuditLog")
        .Produces(StatusCodes.Status200OK);
    }
}
