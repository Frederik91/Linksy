using Linksy.Api.Data;
using Linksy.Migrations;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults (observability, health checks, etc.)
builder.AddServiceDefaults();

// Add PostgreSQL DbContext
builder.AddNpgsqlDbContext<ApplicationDbContext>("linksydb");

// Register the migration service
builder.Services.AddHostedService<MigrationService>();

var host = builder.Build();

await host.RunAsync();
