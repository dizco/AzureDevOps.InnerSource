using System.ComponentModel.DataAnnotations;
using AzureDevOps.Stars.Models;
using AzureDevOps.Stars.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.Stars.Controllers;

public class StarsController : Controller
{
	private readonly IStarService _starService;

	private readonly HttpClient _httpClient;

	public StarsController(IStarService starService, HttpClient httpClient)
	{
		_starService = starService;
		_httpClient = httpClient;
	}

	// NOTE: This endpoint is a GET because it is not possible to make POST requests from a markdown file
	[Authorize]
	[HttpGet("star")]
	public async Task<IActionResult> Star(string project, string repository)
	{
		if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(repository))
		{
			throw new ValidationException("Required parameters were not provided");
		}

		var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectDefaults.AuthenticationScheme, "access_token");
		if (!string.IsNullOrEmpty(accessToken))
		{
			var t = accessToken;
		}

		await _starService.Star(User, new Repository
		{
			Organization = "gabrielbourgault",
			Project = project,
			Name = repository
		});

		// Redirect back to the original repository
		return Redirect($"https://dev.azure.com/gabrielbourgault/{project}/_git/{repository}");
	}

	[HttpGet("stars/{project}/{repositoryName}")]
	public async Task<IActionResult> GetStars(string project, string repositoryName)
	{
		var repository = new Repository
		{
			Organization = "gabrielbourgault",
			Project = project,
			Name = repositoryName
		};
		var users = await _starService.GetStars(repository);

		var shieldsIoUrl = $"https://img.shields.io/static/v1?label=Stars&message={users.Count}&color=informational&logo=azuredevops";
		var badge = await _httpClient.GetAsync(shieldsIoUrl);
		var stream = await badge.Content.ReadAsStreamAsync();
		return File(stream, badge.Content.Headers.ContentType?.ToString() ?? "image/svg+xml;charset=utf-8");
	}
}