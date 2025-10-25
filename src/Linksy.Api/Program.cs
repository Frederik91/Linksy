using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

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

// Sample API endpoints
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

app.MapDefaultEndpoints();

app.Run();
