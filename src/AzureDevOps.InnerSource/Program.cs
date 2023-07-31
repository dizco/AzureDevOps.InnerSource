using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureDevOps.InnerSource;
using AzureDevOps.InnerSource.ADO.Configuration;
using AzureDevOps.InnerSource.ADO.Extensions;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Configuration.Settings;
using AzureDevOps.InnerSource.Exceptions;
using AzureDevOps.InnerSource.Extensions;
using CommandLine;
using Microsoft.IdentityModel.Logging;
using Serilog;

IdentityModelEventSource.ShowPII = Debugger.IsAttached;
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
IConfiguration configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddJsonFile($"appsettings.{aspNetCoreEnvironment}.json", true)
	.AddUserSecrets<Program>()
	.AddEnvironmentVariables()
	.Build();

var command = Parser.Default.ParseArguments<CommandLineOptions>(args);
if (string.Equals(command.Value.Command, "aggregate", StringComparison.OrdinalIgnoreCase))
	await RunAggregationAsync(command.Value);
else
	RunWebMvc();

async Task RunAggregationAsync(CommandLineOptions commandLineOptions)
{
	var host = Host.CreateDefaultBuilder()
		.ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration))
		.ConfigureServices((context, services) =>
		{
			services.AddAzureDevOpsConnection(context.Configuration);
			services.AddRepositoryAggregation(options =>
			{
				var settings = context.Configuration.GetSection(RepositoryAggregationSettings.SectionName).Get<RepositoryAggregationSettings>();
				options.OutputFolder = commandLineOptions.OutputFolder;
				options.BadgeServerUrl = settings.BadgeServerUrl;
				options.Overrides = settings.Overrides?.ToDictionary(x => x.Key,
					x => new RepositoryAggregationOptions.RepositoryAggregationOverride
					{
						Description = x.Value.Description,
						Installation = x.Value.Installation
					}) ?? new Dictionary<string, RepositoryAggregationOptions.RepositoryAggregationOverride>();
			});
		})
		.UseSerilog((context, loggerConfiguration) => loggerConfiguration
			.ReadFrom.Configuration(context.Configuration)
			.Enrich.FromLogContext())
		.Build();

	var aggregator = ActivatorUtilities.CreateInstance<RepositoryAggregator>(host.Services);
	await aggregator.AggregateAsync(CancellationToken.None);
}

void RunWebMvc()
{
	var builder = WebApplication.CreateBuilder(args);
	builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false)
		.ConfigureAppConfiguration(c => c.AddConfiguration(configuration))
		.ConfigureServices((context, services) =>
		{
			services.AddControllersWithViews(options => { options.Filters.Add<ExceptionFilter>(); })
				.AddJsonOptions(options =>
				{
					options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
					options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				});
			services.AddCors(options =>
			{
				options.AddPolicy("AzureDevOpsExtension", policy =>
				{
					policy.AllowCredentials()
						.WithOrigins("https://gabrielbourgault.gallerycdn.vsassets.io")
						.AllowAnyHeader()
						.AllowAnyMethod();
				});
			});
			services.ConfigureAuthentication(context.Configuration);
			services.AddAzureDevOpsConnection(context.Configuration);
			services.AddRepositoryAggregation(options =>
			{
				var settings = context.Configuration.GetSection(RepositoryAggregationSettings.SectionName).Get<RepositoryAggregationSettings>();
				options.BadgeServerUrl = settings.BadgeServerUrl;
				options.Overrides = settings.Overrides?.ToDictionary(x => x.Key,
					x => new RepositoryAggregationOptions.RepositoryAggregationOverride
					{
						Description = x.Value.Description,
						Installation = x.Value.Installation
					}) ?? new Dictionary<string, RepositoryAggregationOptions.RepositoryAggregationOverride>();
			});
			services.AddApplicationServices(context.Configuration);
		});

	builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
		.ReadFrom.Configuration(context.Configuration)
		.Enrich.FromLogContext());

	var app = builder.Build();

	// Configure the HTTP request pipeline.
	if (!app.Environment.IsEnvironment("Local"))
	{
		app.UseExceptionHandler("/Home/Error");
		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
		app.UseHsts();
	}

	app.UseHttpsRedirection();
	app.UseStaticFiles();
	app.UseSerilogRequestLogging();
	app.UseRouting();
	app.UseCors();
	app.UseAuthentication();
	app.UseAuthorization();

	app.MapControllerRoute(
		"default",
		"{controller=Home}/{action=Index}/{id?}");

	app.Run();
}

public partial class Program
{
}