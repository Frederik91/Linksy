var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject("api", @"../Linksy.Api/Linksy.Api.csproj");

builder.Build().Run();
