using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using AzureDevOps.Stars.Exceptions;
using AzureDevOps.Stars.Extensions;
using Microsoft.IdentityModel.Logging;

IdentityModelEventSource.ShowPII = Debugger.IsAttached;
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
IConfiguration configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddJsonFile($"appsettings.{aspNetCoreEnvironment}.json", optional: true)
	.AddEnvironmentVariables()
	.Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
	options.Filters.Add<ExceptionFilter>();
});
builder.Services.ConfigureAuthentication(configuration);
builder.Services.AddStars(configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	"default",
	"{controller=Home}/{action=Index}/{id?}");

app.Run();