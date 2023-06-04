using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Azure.Data.Tables;
using AzureDevOps.Stars.Configuration;
using AzureDevOps.Stars.Configuration.Settings;
using AzureDevOps.Stars.Services;
using AzureDevOps.Stars.Storage;

namespace AzureDevOps.Stars.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddStars(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddHttpClient();

		var devOpsSettings = configuration.GetSection(DevOpsSettings.SectionName).Get<DevOpsSettings>();
		services.AddOptions<DevOpsOptions>()
			.Configure(o =>
			{
				o.Organization = devOpsSettings.Organization;
				o.PersonalAccessToken = devOpsSettings.PersonalAccessToken;
				o.StarsAllowedRepositories = (devOpsSettings.StarsAllowedRepositories ?? new List<DevOpsSettings.StarsAllowedRepositoriesSettings>())
					.Select(x => (Project: x.RegexProject, Repository: x.RegexRepository))
					.ToList();
			})
			.ValidateDataAnnotations();
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
				options.SaveTokens = true; // Specifies whether access_tokens and refresh_tokens should be stored. Required when the app calls APIs
				options.GetClaimsFromUserInfoEndpoint = true;
				options.ClaimActions.MapAll();

				options.Scope.Clear();
				foreach (var scope in new List<string> { "openid", "profile", "email", "offline_access" })
				{
					options.Scope.Add(scope);
				}

				// Specifies which claim type to use for the `User.Identity.Name` and `User.IsInRole()`.
				options.TokenValidationParameters = new TokenValidationParameters
				{
					NameClaimType = "name",
					RoleClaimType = "role"
				};

				options.Events.OnTokenValidated = context =>
				{
					// Issue a token with a fixed expiration
					context.Properties.IsPersistent = true;

					// Set the expiration of the cookie for the same expiration as the access_token
					var accessToken = new JwtSecurityToken(context.TokenEndpointResponse.AccessToken);
					context.Properties.ExpiresUtc = accessToken.ValidTo;
					context.Properties.IssuedUtc = accessToken.ValidFrom;

					return Task.CompletedTask;
				};
			});

		return services;
	}
}