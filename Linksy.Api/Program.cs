using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

builder.Services.AddHealthChecks();
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

// Configure the HTTP request pipeline
app.UseRouting();
app.UseCors("local-dev");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Linksy API starting up");

// Health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(x => x.Key, x => x.Value.Status.ToString()),
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

// Sample API endpoint
app.MapGet("/api/info", () => new
{
    application = "Linksy API",
    version = "0.1.0",
    timestamp = DateTime.UtcNow
})
.WithName("GetInfo")
.WithOpenApi()
.Produces(StatusCodes.Status200OK);

app.Run();
