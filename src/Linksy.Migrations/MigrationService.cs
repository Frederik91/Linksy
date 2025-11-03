using Linksy.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Linksy.Migrations;

/// <summary>
/// Background service that applies database migrations on startup
/// </summary>
public class MigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<MigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting database migration service...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Ensuring database exists...");
            await EnsureDatabaseAsync(dbContext, cancellationToken);

            _logger.LogInformation("Applying pending migrations...");
            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Database migrations completed successfully!");

            // Optional: Seed initial data
            await SeedDataAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");

            // Stop the application if migrations fail
            _hostApplicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task EnsureDatabaseAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Create the database if it doesn't exist
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    private async Task SeedDataAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check if data already exists
        if (await dbContext.Tenants.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already contains data, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding initial data...");

        // Add a demo tenant for development
        var demoTenant = new Api.Models.Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Demo Organization",
            BillingProfile = "Free Tier",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Tenants.Add(demoTenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Initial data seeded successfully!");
    }
}
