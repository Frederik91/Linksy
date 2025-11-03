using Linksy.Api.Data;
using Linksy.Api.DTOs;
using Linksy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Linksy.Api.Endpoints;

public static class SyncJobEndpoints
{
    public static void MapSyncJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync-jobs")
            .WithTags("Sync Jobs")
            .WithOpenApi();

        // Get all sync jobs for a binding
        group.MapGet("/binding/{bindingId:guid}", async (Guid bindingId, ApplicationDbContext db) =>
        {
            var jobs = await db.SyncJobs
                .Where(j => j.BindingId == bindingId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new SyncJobDto(
                    j.Id,
                    j.BindingId,
                    j.Status,
                    j.CreatedAt,
                    j.StartedAt,
                    j.CompletedAt,
                    j.FilesProcessed,
                    j.BytesTransferred,
                    j.ConflictsDetected,
                    j.Retries,
                    j.ErrorMessage,
                    j.IsManualTrigger
                ))
                .ToListAsync();

            return Results.Ok(jobs);
        })
        .WithName("GetSyncJobsByBinding")
        .Produces<List<SyncJobDto>>(StatusCodes.Status200OK);

        // Get sync job by ID
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var job = await db.SyncJobs.FindAsync(id);

            if (job is null)
                return Results.NotFound(new { message = "Sync job not found" });

            var jobDto = new SyncJobDto(
                job.Id,
                job.BindingId,
                job.Status,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt,
                job.FilesProcessed,
                job.BytesTransferred,
                job.ConflictsDetected,
                job.Retries,
                job.ErrorMessage,
                job.IsManualTrigger
            );

            return Results.Ok(jobDto);
        })
        .WithName("GetSyncJobById")
        .Produces<SyncJobDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Create/trigger sync job
        group.MapPost("/", async (CreateSyncJobRequest request, ApplicationDbContext db) =>
        {
            // Validate binding exists and is active
            var binding = await db.Bindings.FindAsync(request.BindingId);

            if (binding is null)
                return Results.BadRequest(new { message = "Binding not found" });

            if (!binding.IsActive || binding.IsPaused)
                return Results.BadRequest(new { message = "Binding is not active or is paused" });

            var job = new SyncJob
            {
                Id = Guid.NewGuid(),
                BindingId = request.BindingId,
                Status = SyncJobStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IsManualTrigger = request.IsManualTrigger,
                FilesProcessed = 0,
                BytesTransferred = 0,
                ConflictsDetected = 0,
                Retries = 0
            };

            db.SyncJobs.Add(job);
            await db.SaveChangesAsync();

            var jobDto = new SyncJobDto(
                job.Id,
                job.BindingId,
                job.Status,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt,
                job.FilesProcessed,
                job.BytesTransferred,
                job.ConflictsDetected,
                job.Retries,
                job.ErrorMessage,
                job.IsManualTrigger
            );

            return Results.Created($"/api/sync-jobs/{job.Id}", jobDto);
        })
        .WithName("CreateSyncJob")
        .Produces<SyncJobDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Get sync job statistics for a tenant
        group.MapGet("/tenant/{tenantId:guid}/stats", async (Guid tenantId, ApplicationDbContext db) =>
        {
            var jobs = await db.SyncJobs
                .Where(j => j.Binding.TenantId == tenantId)
                .ToListAsync();

            var stats = new SyncJobStatsDto(
                TotalJobs: jobs.Count,
                CompletedJobs: jobs.Count(j => j.Status == SyncJobStatus.Completed),
                FailedJobs: jobs.Count(j => j.Status == SyncJobStatus.Failed),
                RunningJobs: jobs.Count(j => j.Status == SyncJobStatus.Running),
                TotalBytesTransferred: jobs.Sum(j => j.BytesTransferred),
                TotalFilesProcessed: jobs.Sum(j => j.FilesProcessed)
            );

            return Results.Ok(stats);
        })
        .WithName("GetSyncJobStats")
        .Produces<SyncJobStatsDto>(StatusCodes.Status200OK);

        // Cancel sync job
        group.MapPost("/{id:guid}/cancel", async (Guid id, ApplicationDbContext db) =>
        {
            var job = await db.SyncJobs.FindAsync(id);

            if (job is null)
                return Results.NotFound(new { message = "Sync job not found" });

            if (job.Status == SyncJobStatus.Completed || job.Status == SyncJobStatus.Failed || job.Status == SyncJobStatus.Cancelled)
                return Results.BadRequest(new { message = "Cannot cancel job in current status" });

            job.Status = SyncJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Sync job cancelled successfully" });
        })
        .WithName("CancelSyncJob")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
