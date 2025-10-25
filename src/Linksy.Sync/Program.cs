using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults();

builder.ConfigureServices(services =>
{
    // Register Sync orchestrator interface (placeholder for Phase 1 implementation)
    // services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
});

var host = builder.Build();

host.Run();
