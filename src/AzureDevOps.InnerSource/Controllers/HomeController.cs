using System.Diagnostics;
using AzureDevOps.Stars.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.Stars.Controllers;

public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger)
	{
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