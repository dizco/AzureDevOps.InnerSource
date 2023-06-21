// See https://aka.ms/new-console-template for more information

using AzureDevOps.InnerSource.RepositoryAggregator.Extensions;
using AzureDevOps.InnerSource.RepositoryAggregator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine($"Hello, World! {args.Length}");

var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
IConfiguration configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddJsonFile($"appsettings.{aspNetCoreEnvironment}.json", true)
	.AddUserSecrets<Program>()
	.AddEnvironmentVariables()
	.Build();

var services = new ServiceCollection();
services.AddRepositoryAggregation(configuration);
await using var provider = services.BuildServiceProvider();

var aggregator = provider.GetRequiredService<RepositoryAggregator>();
await aggregator.AggregateAsync(CancellationToken.None);
