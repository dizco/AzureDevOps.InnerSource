using AzureDevOps.InnerSource.RepositoryAggregator.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureDevOps.InnerSource.RepositoryAggregator.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRepositoryAggregation(this IServiceCollection services, Action<RepositoryAggregationOptions> configure)
	{
		services.AddOptions<RepositoryAggregationOptions>()
			.Configure(configure)
			.ValidateDataAnnotations();

		services.AddTransient<Services.RepositoryAggregator>();

		return services;
	}
}