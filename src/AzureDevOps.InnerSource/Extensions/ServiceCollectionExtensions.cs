using System.Net;
using System.Text;
using Azure.Data.Tables;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Configuration;
using AzureDevOps.InnerSource.Configuration.Settings;
using AzureDevOps.InnerSource.Services;
using AzureDevOps.InnerSource.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
		services.AddOptions<AuthenticationOptions>()
			.Configure(o =>
			{
				o.Key = authenticationSettings.Key;
				o.Issuer = authenticationSettings.Issuer;
				o.Audience = authenticationSettings.Audience;
			})
			.ValidateDataAnnotations();

		services.AddTransient<ITokenService, TokenService>();

		services.AddAuthentication(options =>
			{
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = "AzureDevOpsExtension";
			})
			.AddJwtBearer("AzureDevOpsExtension", options =>
			{
				// Allows the Azure DevOps extension tokens
				// See: https://learn.microsoft.com/en-us/azure/devops/extend/develop/auth?view=azure-devops#net-framework

				//var secret =
				//	"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im9PdmN6NU1fN3AtSGpJS2xGWHo5M3VfVjBabyJ9.eyJjaWQiOiIwMzZjMjViNy02MzlmLTQyOGEtOTlkYy0zMzUwYWJjNTE1ZGEiLCJjc2kiOiJhZDU1OTc2YS05OTgyLTRiNTMtYWMzMS01YjVmMjA4NjQ0YjUiLCJuYW1laWQiOiIwMDAwMDAyOS0wMDAwLTg4ODgtODAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJhcHAudnN0b2tlbi52aXN1YWxzdHVkaW8uY29tIiwiYXVkIjoiYXBwLnZzdG9rZW4udmlzdWFsc3R1ZGlvLmNvbSIsIm5iZiI6MTY4ODI0OTU3MSwiZXhwIjoxODQ2MTAyMzcxfQ.HYFSVshJhozP9t1fhDniSgN-AJT2dSuoeutJPex907xxZTZ1zK8sJq9rPOkZmMM1Q-3d0xoYorlw1rHKTE7LYJAAFhf7q2YvgWnFYpYovwNWFnxyquCWR5E_dEyLMxpq2RkjjOGxMRZbgXpOGxCBhUUp00-dBU4bYRVA7moIAx-iiK8xrh8BMgtjDj1czKPhRTnIrPsAGmu4GE6cV7acXoaSlM0V3bTHZRn_IllnwqOv3dEUFVsA3O0SWjCBwRR0nYPrYOitRj5GN5VMfIOPm8WfbZDzXSKU72PHMO2cA_O8CrlBk9HRcgBPtB1wYl6Tm4eq2Ps8DGMtmg9GD2g0xw";
				var secret =
					"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im9PdmN6NU1fN3AtSGpJS2xGWHo5M3VfVjBabyJ9.eyJjaWQiOiJjNWQzNmM0OC1kMTE4LTQ5ODItOTRlMS1kNzc2OTczNjYxOTEiLCJjc2kiOiJjYzBlODNjNC0yODZmLTRhMjUtODc5Ni05NmE2NGQwNmUzNDYiLCJuYW1laWQiOiIwMDAwMDAyOS0wMDAwLTg4ODgtODAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJhcHAudnN0b2tlbi52aXN1YWxzdHVkaW8uY29tIiwiYXVkIjoiYXBwLnZzdG9rZW4udmlzdWFsc3R1ZGlvLmNvbSIsIm5iZiI6MTY4ODkzNTUyMiwiZXhwIjoxODQ2Nzg4MzIyfQ.MlLAEE3yDsX0CDNtV0L0pXJS909Vd5PcHVuFXFShUMD9UAfiaZnAloKULeqahKAdvnx2bCc_wKzvkqEYIDPQy_vBgm87ANrgFhuKYkx2zWn-EwedMIl3oHDrQ4QKhL9nEjznP4vC8BSHHuIsG4-YUJtX0JPEjCVzWkUyJpw3JpD-33H3tjpBVg76dZyBdO4jCne-jc7JSLneYn4LW4lfhsL3j-7r9Lq6X7Puj7LypUSKPQHGV0Pt5jy95mQmG-p1DyLE9c4WzCpYOmueYZWTtaon32Mq-mAYSM-_AVHgR7XjU42r_frf5MFREAYIxbWoohrQFPIrwyWo-XNr064t-g";

				options.TokenValidationParameters = new TokenValidationParameters
				{
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
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

				options.Events = new JwtBearerEvents
				{
					OnAuthenticationFailed = ctx =>
					{
						var t = 0;
						return Task.CompletedTask;
					},
					OnChallenge = ctx =>
					{
						var t = 1;
						return Task.CompletedTask;
					},
					OnForbidden = ctx =>
					{
						var t = 2;
						return Task.CompletedTask;
					},
					OnMessageReceived = ctx =>
					{
						var t = 3;
						return Task.CompletedTask;
					},
					OnTokenValidated = ctx =>
					{
						var t = 4;
						return Task.CompletedTask;
					}
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

			// TODO: Define authorization policies
			/*options.AddPolicy("BadgeReader", builder => builder.RequireAuthenticatedUser()
				.RequireClaim("scope", "badges.read"));*/
		});

		return services;
	}
}