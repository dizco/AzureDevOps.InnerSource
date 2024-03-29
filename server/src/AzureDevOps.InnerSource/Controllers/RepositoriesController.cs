﻿using System.ComponentModel.DataAnnotations;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.InnerSource.Controllers;

public class RepositoriesController : Controller
{
	private readonly RepositoryService _repositoryService;

	public RepositoriesController(RepositoryService repositoryService)
	{
		_repositoryService = repositoryService;
	}

	[Authorize]
	[EnableCors("AzureDevOpsExtension")]
	[HttpGet("{projectId}/repositories")]
	[ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Client)]
	public async Task<IActionResult> GetRepositories(string projectId, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(projectId))
			throw new ValidationException("Required parameters were not provided");

		var repositories = await _repositoryService.GetRepositoriesAsync(projectId, ct);

		return Json(new
		{
			repositories
		});
	}
}