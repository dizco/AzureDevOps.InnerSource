using System.ComponentModel.DataAnnotations;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.InnerSource.Controllers;

public class BadgesController : Controller
{
    private readonly BadgeService _badgeService;
    private readonly RepositoryHealthService _repositoryHealthService;

    private readonly ILogger<BadgesController> _logger;

    public BadgesController(BadgeService badgeService, RepositoryHealthService repositoryHealthService, ILogger<BadgesController> logger)
    {
        _badgeService = badgeService;
        _repositoryHealthService = repositoryHealthService;
        _logger = logger;
    }
    
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},AzureDevOpsBadge")]
    [HttpGet("{projectName}/repositories/{repositoryId}/badges/last-commit")]
    [EnableCors("AzureDevOpsExtension")]
	public async Task<IActionResult> GetLastCommit(string projectName, string repositoryId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
            throw new ValidationException("Required parameters were not provided");

        if (!User.HasClaim(x => string.Equals(x.Type, "sub", StringComparison.OrdinalIgnoreCase))
            && !User.HasClaim("repositoryId", repositoryId))
        {
            _logger.LogError("Badge access token is not permitted for the requested repository {repositoryId}", repositoryId);
	        return Forbid();
        }

        var lastCommitDate = await _repositoryHealthService.GetLastCommitDateAsync(Guid.Parse(repositoryId), ct);
        var humanReadableDate = "never";
        if (lastCommitDate.HasValue)
        {
            var daysElapsed = (DateTime.UtcNow - lastCommitDate.Value).Days;
            humanReadableDate = daysElapsed switch
            {
                <= 0 => "today",
                <= 1 => "yesterday",
                <= 30 => $"{daysElapsed} days ago",
                <= 180 => lastCommitDate.Value.ToString("MMMM"),
                _ => lastCommitDate.Value.ToString("MMMM yyyy")
            };
        }

        var color = _badgeService.GetColorByAge(lastCommitDate);
        return await _badgeService.CreateAsync("last commit", humanReadableDate, color, ct: ct);
    }
}