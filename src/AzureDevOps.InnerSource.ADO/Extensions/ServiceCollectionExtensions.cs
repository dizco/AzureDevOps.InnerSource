using AzureDevOps.InnerSource.ADO.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureDevOps.InnerSource.ADO.Extensions;

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