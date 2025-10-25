var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject("api", @"../Linksy.Api/Linksy.Api.csproj");

var frontend = builder.AddNpmApp("frontend", @"../frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
