using System.ComponentModel.DataAnnotations;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Common.Configuration;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Controllers;

[Route("repositories")]
public class RepositoriesController : Controller
{
	private readonly IOptionsMonitor<DevOpsOptions> _options;

	private readonly RepositoryService _repositoryService;

	public RepositoriesController(IOptionsMonitor<DevOpsOptions> options, RepositoryService repositoryService)
	{
		_options = options;
		_repositoryService = repositoryService;
	}

	private DevOpsOptions Options => _options.CurrentValue;

	//[Authorize] // TODO: Set authorization
	[EnableCors("AzureDevOpsExtension")]
	[HttpGet("{projectId}")]
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