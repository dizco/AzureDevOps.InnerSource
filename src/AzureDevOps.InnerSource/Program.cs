using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using AzureDevOps.InnerSource;
using AzureDevOps.InnerSource.ADO.Extensions;
using AzureDevOps.InnerSource.ADO.Services;
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
    await RunAggregationAsync();
else
    RunWebMvc();

async Task RunAggregationAsync()
{
    var services = new ServiceCollection();
    services.AddAzureDevOpsConnection(configuration);
    services.AddRepositoryAggregation(options => { options.OutputFolder = command.Value.OutputFolder; });
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
    builder.Services.AddControllersWithViews(options => { options.Filters.Add<ExceptionFilter>(); });
    builder.Services.ConfigureAuthentication(configuration);
    builder.Services.AddAzureDevOpsConnection(configuration);
    builder.Services.AddStars(configuration);
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsEnvironment("Local"))
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}

public partial class Program
{
}