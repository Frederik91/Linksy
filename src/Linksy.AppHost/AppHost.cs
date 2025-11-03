var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("linksydb");

var api = builder.AddProject<Projects.Linksy_Api>("api")
    .WithReference(postgres);

var frontend = builder.AddNpmApp("frontend", @"../frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
