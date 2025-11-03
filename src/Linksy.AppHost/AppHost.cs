var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("linksydb");

// Add migrations service - runs first to set up the database
var migrations = builder.AddProject<Projects.Linksy_Migrations>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

// Add API - waits for migrations to complete
var api = builder.AddProject<Projects.Linksy_Api>("api")
    .WithReference(postgres)
    .WaitFor(migrations);

// Add frontend - waits for API
var frontend = builder.AddNpmApp("frontend", @"../frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
