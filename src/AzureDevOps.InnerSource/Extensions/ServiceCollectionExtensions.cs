using Azure.Data.Tables;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.Stars.Configuration.Settings;
using AzureDevOps.Stars.Services;
using AzureDevOps.Stars.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.Stars.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAzureDevOpsConnection(this IServiceCollection services, IConfiguration configuration)
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

		return services;
	}

	public static IServiceCollection AddStars(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddHttpClient();
		services.AddSingleton<IStarService, StarService>();

		var storage = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>();
		switch (storage.Mode)
		{
			case "TableStorage":
				services.AddSingleton<IStarRepository, StarTableRepository>();
				services.AddSingleton(_ => new TableServiceClient(storage.TableStorageConnectionString).GetTableClient(storage.TableName));
				break;
			case "InMemory":
				services.AddSingleton<IStarRepository, StarInMemoryRepository>();
				break;
			default:
				throw new Exception($"Storage mode {storage.Mode} not supported.");
		}


		return services;
	}

	public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
			})
			.AddCookie()
			.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
			{
				var settings = configuration.GetSection(IdentityProviderSettings.SectionName).Get<IdentityProviderSettings>();

				options.Authority = settings.Authority;
				options.ClientId = settings.ClientId;
				options.ClientSecret = settings.ClientSecret;
				options.ResponseType = "code";
				options.SaveTokens = true;
				options.GetClaimsFromUserInfoEndpoint = true;
				options.ClaimActions.MapAll();

				options.Scope.Clear();
				foreach (var scope in new List<string> { "openid", "email", "offline_access" }) options.Scope.Add(scope);
			});

		return services;
	}
}