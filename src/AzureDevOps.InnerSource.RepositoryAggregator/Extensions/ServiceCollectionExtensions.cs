using AzureDevOps.InnerSource.RepositoryAggregator.Configuration;
using AzureDevOps.InnerSource.RepositoryAggregator.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.RepositoryAggregator.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRepositoryAggregation(this IServiceCollection services, IConfiguration configuration)
	{
		var devOpsSettings = configuration.GetSection(DevOpsSettings.SectionName).Get<DevOpsSettings>();
		services.AddOptions<DevOpsOptions>()
			.Configure(o =>
			{
				o.Organization = devOpsSettings.Organization;
				o.PersonalAccessToken = devOpsSettings.PersonalAccessToken;
				o.AllowedRepositories = (devOpsSettings.AllowedRepositories ?? new List<DevOpsSettings.AllowedRepositoriesSettings>())
					.Select(x => (Project: x.RegexProject, Repository: x.RegexRepository))
					.ToList();
			})
			.ValidateDataAnnotations();


		services.AddSingleton(s =>
		{
			var options = s.GetRequiredService<IOptionsSnapshot<DevOpsOptions>>().Value;
			var organizationUrl = new Uri($"https://dev.azure.com/{options.Organization}");
			return new VssConnection(organizationUrl, new VssBasicCredential("", options.PersonalAccessToken));
		});

		services.AddTransient<Services.RepositoryAggregator>();

		return services;
	}
}