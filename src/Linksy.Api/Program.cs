using Linksy.Api.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<ApplicationDbContext>("linksydb");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("local-dev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseCors("local-dev");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map API endpoints
app.MapGet("/api/info", () => new
{
    application = "Linksy API",
    version = "0.1.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
})
.WithName("GetInfo")
.WithOpenApi()
.Produces(StatusCodes.Status200OK);

// Register all feature endpoints
Linksy.Api.Endpoints.TenantEndpoints.MapTenantEndpoints(app);
Linksy.Api.Endpoints.ConnectorEndpoints.MapConnectorEndpoints(app);
Linksy.Api.Endpoints.BindingEndpoints.MapBindingEndpoints(app);
Linksy.Api.Endpoints.SyncJobEndpoints.MapSyncJobEndpoints(app);
Linksy.Api.Endpoints.AuditEndpoints.MapAuditEndpoints(app);

app.MapDefaultEndpoints();

app.Run();
