using System.ComponentModel.DataAnnotations;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.InnerSource.Controllers;

[Route("badges")]
public class BadgesController : Controller
{
    private readonly BadgeService _badgeService;
    private readonly RepositoryHealthService _repositoryHealthService;

    public BadgesController(BadgeService badgeService, RepositoryHealthService repositoryHealthService)
    {
        _badgeService = badgeService;
        _repositoryHealthService = repositoryHealthService;
    }
    
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},AzureDevOpsBadge")]
    [HttpGet("last-commit/{repositoryId}")]
    public async Task<IActionResult> GetLastCommit(string repositoryId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
            throw new ValidationException("Required parameters were not provided");

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