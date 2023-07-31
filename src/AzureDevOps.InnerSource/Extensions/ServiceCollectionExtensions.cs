using System.Net;
using System.Text;
using Azure.Data.Tables;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Common.Services;
using AzureDevOps.InnerSource.Configuration;
using AzureDevOps.InnerSource.Configuration.Settings;
using AzureDevOps.InnerSource.Services;
using AzureDevOps.InnerSource.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.Extensions;

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

	public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddHttpClient();
		services.AddHttpContextAccessor();
		services.AddMemoryCache();
		services.AddTransient<IPrincipalService, PrincipalService>();
		services.AddTransient<BadgeService>();
		services.AddTransient<RepositoryService>();
		services.AddStars(configuration);
		return services;
	}

	public static IServiceCollection AddBadgeTokenService(this IServiceCollection services, IConfiguration configuration)
	{
		var authenticationSettings = configuration.GetRequiredSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>();
		services.AddOptions<AuthenticationOptions>()
			.Configure(o =>
			{
				o.Key = authenticationSettings.Key;
				o.Issuer = authenticationSettings.Issuer;
				o.Audience = authenticationSettings.Audience;
			})
			.ValidateDataAnnotations();

		services.AddTransient<IBadgeTokenService, TokenService>();
		return services;
	}

	private static IServiceCollection AddStars(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddTransient<IStarService, StarService>();

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
		// Note-gbourgault: The obviously safer and cleaner approach to authentication would be to leverage cookie authentication with the host
		// however, it does not work within the Azure DevOps iframe (even with SameSite=none cookies).
		// Our workaround is to generate our own tokens that the frontend can use during API calls.

		var authenticationSettings = configuration.GetRequiredSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>();

		services.AddTransient<ITokenService, TokenService>();

		services.AddAuthentication(options =>
			{
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer("AzureDevOpsExtension", options =>
			{
				// Allows the Azure DevOps extension tokens
				// See: https://learn.microsoft.com/en-us/azure/devops/extend/develop/auth?view=azure-devops#net-framework

				options.TokenValidationParameters = new TokenValidationParameters
				{
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationSettings.AzureDevOpsKey)),
					ValidateIssuer = false,
					RequireSignedTokens = true,
					RequireExpirationTime = true,
					ValidateLifetime = true,
					ValidateAudience = false,
					ValidateActor = false
				};
			})
			.AddJwtBearer(options =>
			{
				// Allows our tokens
				options.TokenValidationParameters = new TokenValidationParameters
				{
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationSettings.Key)),
					ValidateIssuer = true,
					ValidIssuer = authenticationSettings.Issuer,
					RequireSignedTokens = true,
					RequireExpirationTime = true,
					ValidateLifetime = true,
					ValidateAudience = true,
					ValidAudience = authenticationSettings.Audience
				};
			})
			.AddJwtBearer("AzureDevOpsBadge", options =>
			{
				// Because badges are displayed as images, this authentication scheme looks for an "access_token" in the query string

				options.TokenValidationParameters = new TokenValidationParameters
				{
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationSettings.Key)),
					ValidateIssuer = true,
					ValidIssuer = authenticationSettings.Issuer,
					RequireSignedTokens = true,
					RequireExpirationTime = true,
					ValidateLifetime = true,
					ValidateAudience = true,
					ValidAudience = authenticationSettings.Audience
				};

				// Source: https://stackoverflow.com/a/53295042/6316091
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context => {
						if (!context.Request.Query.TryGetValue("access_token", out var values))
						{
							return Task.CompletedTask;
						}

						if (values.Count > 1)
						{
							context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
							context.Fail($"Only one 'access_token' query string parameter can be defined. However, {values.Count:N0} were included in the request.");
							return Task.CompletedTask;
						}

						var token = values.Single();

						if (string.IsNullOrWhiteSpace(token))
						{
							context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
							context.Fail("The 'access_token' query string parameter was defined, but a value to represent the token was not included.");
							return Task.CompletedTask;
						}

						context.Token = token;
						return Task.CompletedTask;
					}
				};
			});

		services.AddAuthorization(options =>
		{
			options.DefaultPolicy = new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();
		});

		return services;
	}
}