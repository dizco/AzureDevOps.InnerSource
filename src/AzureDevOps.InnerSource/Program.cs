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
    var services = new ServiceCollection();
    services.AddAzureDevOpsConnection(configuration);
    services.AddRepositoryAggregation(options =>
    {
        var settings = configuration.GetSection(RepositoryAggregationSettings.SectionName).Get<RepositoryAggregationSettings>();
        options.OutputFolder = commandLineOptions.OutputFolder;
        options.BadgeServerUrl = settings.BadgeServerUrl;
        options.Overrides = settings.Overrides?.ToDictionary(x => x.Key,
            x => new RepositoryAggregationOptions.RepositoryAggregationOverride
            {
                Description = x.Value.Description,
                Installation = x.Value.Installation
            }) ?? new Dictionary<string, RepositoryAggregationOptions.RepositoryAggregationOverride>();
    });
#pragma warning disable ASP0000
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000

    var aggregator = provider.GetRequiredService<RepositoryAggregator>();
    await aggregator.AggregateAsync(CancellationToken.None);
}

void RunWebMvc()
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllersWithViews(options => { options.Filters.Add<ExceptionFilter>(); })
	    .AddJsonOptions(options =>
	    {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	    });
    builder.Services.AddCors(options =>
    {
	    options.AddPolicy("AzureDevOpsExtension", policy =>
	    {
			// TODO: Could probably be a bit more restrictive
			policy.AllowCredentials()
				.WithOrigins("https://gabrielbourgault.gallerycdn.vsassets.io")
				.AllowAnyHeader()
				.AllowAnyMethod();
	    });
    });
    builder.Services.ConfigureAuthentication(configuration);
    builder.Services.AddAzureDevOpsConnection(configuration);
    builder.Services.AddRepositoryAggregation(options =>
    {
	    var settings = configuration.GetSection(RepositoryAggregationSettings.SectionName).Get<RepositoryAggregationSettings>();
	    options.BadgeServerUrl = settings.BadgeServerUrl;
	    options.Overrides = settings.Overrides?.ToDictionary(x => x.Key,
		    x => new RepositoryAggregationOptions.RepositoryAggregationOverride
		    {
			    Description = x.Value.Description,
			    Installation = x.Value.Installation
		    }) ?? new Dictionary<string, RepositoryAggregationOptions.RepositoryAggregationOverride>();
    });
	builder.Services.AddApplicationServices(configuration);
    builder.Services.AddRepositoryHealth();
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