using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var env = builder.Environment;

builder.AddAzureContainerAppEnvironment("env");

var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> sample = null;
if (env.IsProduction())
{
    sample = postgres.AddDatabase("sample");
}
else
{
    if (builder.ExecutionContext.IsRunMode)
    {
        sample = postgres.RunAsContainer(container =>
        {
            container.WithPgAdmin();
            container.WithDataVolume();
            container.WithLifetime(ContainerLifetime.Persistent);
        }).AddDatabase("sample");
    }
    else
    {
        sample = postgres.RunAsContainer(container =>
        {
            container.WithPgAdmin();
            container.WithLifetime(ContainerLifetime.Session);
        }).AddDatabase("sample");
    }
}

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.AspireDashboardBI_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireDashboardBI_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(sample)
    .WaitFor(sample);

builder.Build().Run();
