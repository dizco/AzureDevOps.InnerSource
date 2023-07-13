using System.ComponentModel.DataAnnotations;
using AzureDevOps.InnerSource.Common.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Controllers;

[Route("repositories")]
public class RepositoriesController : Controller
{
	private readonly IOptionsMonitor<DevOpsOptions> _options;

	public RepositoriesController(IOptionsMonitor<DevOpsOptions> options)
	{
		_options = options;
	}

	private DevOpsOptions Options => _options.CurrentValue;

	[Authorize]
	[EnableCors("AzureDevOpsExtension")]
	[HttpGet]
	public async Task<IActionResult> GetRepositories(string project)
	{
		if (string.IsNullOrWhiteSpace(project))
			throw new ValidationException("Required parameters were not provided");

		await Task.CompletedTask;

		return Json(new
		{
			repositories = new List<dynamic>
			{
				new { name = "myrepo", description = "mydescription" },
				new { name = "myrepo2", description = "mydescription2" },
			}
		});
	}
}