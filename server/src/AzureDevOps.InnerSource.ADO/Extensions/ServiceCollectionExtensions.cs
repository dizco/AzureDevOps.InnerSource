﻿using AzureDevOps.InnerSource.ADO.Configuration;
using AzureDevOps.InnerSource.ADO.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AzureDevOps.InnerSource.ADO.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRepositoryAggregation(this IServiceCollection services, Action<RepositoryAggregationOptions> configure)
	{
		services.AddOptions<RepositoryAggregationOptions>()
			.Configure(configure)
			.ValidateDataAnnotations();

		services.AddTransient<RepositoryAggregator>();

		services.AddRepositoryHealth();

		return services;
	}

    private static IServiceCollection AddRepositoryHealth(this IServiceCollection services)
    {
        services.AddTransient<RepositoryHealthService>();
		return services;
    }
}