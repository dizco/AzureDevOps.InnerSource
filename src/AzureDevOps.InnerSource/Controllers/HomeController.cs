using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using AzureDevOps.InnerSource.Models;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;

namespace AzureDevOps.InnerSource.Controllers;

public class HomeController : Controller
{
	private readonly IFlurlClient _flurlClient;
	private readonly ILogger<HomeController> _logger;

	public HomeController(HttpClient httpClient, ILogger<HomeController> logger)
	{
		_flurlClient = new FlurlClient(httpClient);
		_logger = logger;
	}

	public async Task<IActionResult> Index()
	{
		if (User.Identity?.IsAuthenticated == true)
		{
			var accessToken =
				await HttpContext.GetTokenAsync(OpenIdConnectDefaults.AuthenticationScheme, "access_token");
			if (!string.IsNullOrEmpty(accessToken))
			{
				var t = accessToken;
			}
		}

		return View();
	}

	[Route("testauth2")]
	public async Task<IActionResult> TestAuth2()
	{
		return View("Index");
	}

	[Route("testauth")]
	[Authorize(AuthenticationSchemes = "AzureDevOpsExtension")]
	[EnableCors("AzureDevOpsExtension")]
	public async Task<IActionResult> TestAuth()
	{
		var result = await HttpContext.AuthenticateAsync("AzureDevOpsExtension");

		if (HttpContext.Request.Headers.TryGetValue("X-AzureDevOps-AccessToken", out StringValues userAccessToken))
		{
			// We have a user access token
			var connection = await _flurlClient.Request("https://dev.azure.com/gabrielbourgault/_apis/connectionData")
				.WithOAuthBearerToken(userAccessToken.ToString())
				.GetJsonAsync<dynamic>();
		}

		return View("Index");
	}

	public IActionResult Privacy()
	{
		return View();
	}

	public IActionResult Login()
	{
		return Challenge(new OpenIdConnectChallengeProperties
		{
			RedirectUri = "/Home/Index"
		}, OpenIdConnectDefaults.AuthenticationScheme);
	}

	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync();

		return RedirectToAction("Index", "Home");
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}