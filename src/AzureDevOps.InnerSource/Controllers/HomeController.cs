using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AzureDevOps.InnerSource.Models;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

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

	[Route("authenticate")]
	[Authorize(AuthenticationSchemes = "AzureDevOpsExtension")]
	[EnableCors("AzureDevOpsExtension")]
	public async Task<IActionResult> Authenticate()
	{
		var nameid = HttpContext.User.FindFirstValue("nameid");
		var result = await HttpContext.AuthenticateAsync("AzureDevOpsExtension");

		if (HttpContext.Request.Headers.TryGetValue("X-AzureDevOps-AccessToken", out StringValues userAccessToken))
		{
			// TODO: Should we validate that the nameid in the userAccessToken == the nameid in the app token?
			// We have a user access token
			var connection = await _flurlClient.Request("https://dev.azure.com/gabrielbourgault/_apis/connectionData")
				.WithOAuthBearerToken(userAccessToken.ToString())
				.GetJsonAsync<dynamic>();

			var credential = new VssOAuthAccessTokenCredential(userAccessToken.ToString());
			var conn = new VssConnection(new Uri("https://dev.azure.com/gabrielbourgault"), credential);
			await conn.ConnectAsync();

			if (string.Equals(conn.AuthenticatedIdentity.Descriptor.IdentityType, IdentityConstants.System_PublicAccess, StringComparison.Ordinal))
			{
				_logger.LogError("Access token is invalid, public access is unauthorized");
				return Unauthorized();
			}

			// No need to actually validate any of the access token properties because this is done by the Vss Connection above
			var tokenHandler = new JwtSecurityTokenHandler();
			JwtSecurityToken token = tokenHandler.ReadJwtToken(userAccessToken.ToString());
			var accessTokenNameId = token.Claims.FirstOrDefault(x => x.Type == "nameid")?.Value;

			if (!string.Equals(nameid, accessTokenNameId, StringComparison.Ordinal))
			{
				_logger.LogError("Bearer token identity {bearerIdentity} does not match access token identity {accessTokenIdentity}", nameid, accessTokenNameId);
				return Unauthorized();
			}

			var userId = conn.AuthenticatedIdentity.Id;
			_logger.LogInformation("Authenticated with user id {userId}", userId);
		}
		else
		{
			_logger.LogError("User access token not found in request headers");
			return Unauthorized();
		}

		return Json(new
		{
			ok = true
		});
	}

	[Route("token")]
	//[Authorize(AuthenticationSchemes = "AzureDevOpsExtensionCookie")] // TODO: Expect cookie authentication?
	[EnableCors("AzureDevOpsExtension")]
	public async Task<IActionResult> Token()
	{
		// TODO: Generate a JWT
		await Task.CompletedTask;

		return Json(new
		{
			Token = "myjwt"
		});
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