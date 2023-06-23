﻿using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Controllers;

public class StarsController : Controller
{
	private readonly HttpClient _httpClient;
	private readonly IOptionsMonitor<DevOpsOptions> _options;
	private readonly IStarService _starService;

	public StarsController(IStarService starService, IOptionsMonitor<DevOpsOptions> options, HttpClient httpClient)
	{
		_starService = starService;
		_options = options;
		_httpClient = httpClient;
	}

	private DevOpsOptions Options => _options.CurrentValue;

	// NOTE: This endpoint is a GET because it is not possible to make POST requests from a markdown file
	[Authorize]
	[HttpGet("star")]
	public async Task<IActionResult> Star(string project, string repository)
	{
		if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(repository))
			throw new ValidationException("Required parameters were not provided");

		var principal = new Principal
		{
			Id = User.FindFirstValue("oid") ?? throw new Exception("Expected to find an oid claim"),
			Email = User.FindFirstValue("email")
		};

		await _starService.StarAsync(principal, new Repository
		{
			Organization = Options.Organization,
			Project = project,
			Name = repository
		});

		// Redirect back to the original repository
		return Redirect($"https://dev.azure.com/{Options.Organization}/{project}/_git/{repository}");
	}

	[HttpGet("stars/{project}/{repositoryName}")]
	public async Task<IActionResult> GetStars(string project, string repositoryName)
	{
		if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(repositoryName))
			throw new ValidationException("Required parameters were not provided");

		var repository = new Repository
		{
			Organization = Options.Organization,
			Project = project,
			Name = repositoryName
		};
		var stars = await _starService.GetStarCountAsync(repository);

		var shieldsIoUrl = $"https://img.shields.io/static/v1?label=Stars&message={stars}&color=informational&logo=azuredevops";
		var badge = await _httpClient.GetAsync(shieldsIoUrl);
		var stream = await badge.Content.ReadAsStreamAsync();
		return File(stream, badge.Content.Headers.ContentType?.ToString() ?? "image/svg+xml;charset=utf-8");
	}
}