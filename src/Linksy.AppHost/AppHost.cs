var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage emulator for Functions and local development
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

// Add API service
var api = builder.AddProject("api", @"../Linksy.Api/Linksy.Api.csproj");

// Add Sync Functions project with storage emulator
var syncFunctions = builder.AddAzureFunctionsProject<Projects.Linksy_Sync>("sync")
    .WithHostStorage(storage)
    .WithExternalHttpEndpoints();

// Add frontend React app with references to API and Functions
var frontend = builder.AddNpmApp("frontend", @"../frontend")
    .WithReference(api)
    .WithReference(syncFunctions)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .WithEnvironment("VITE_SYNC_URL", syncFunctions.GetEndpoint("http"))
    .WaitFor(api)
    .WaitFor(syncFunctions);

builder.Build().Run();
