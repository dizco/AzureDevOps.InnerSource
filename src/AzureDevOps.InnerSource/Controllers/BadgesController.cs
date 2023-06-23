using System.ComponentModel.DataAnnotations;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzureDevOps.InnerSource.Controllers;

[Route("badges")]
public class BadgesController : Controller
{
    private readonly RepositoryHealthService _repositoryHealthService;
    private readonly BadgeService _badgeService;

    public BadgesController(BadgeService badgeService, RepositoryHealthService repositoryHealthService)
    {
        _badgeService = badgeService;
        _repositoryHealthService = repositoryHealthService;
    }

    // Could possibly expose [HttpGet("last-commit/{project}/{repositoryName}")], but should think about security impacts 
    [HttpGet("last-commit/{repositoryId}")]
    public async Task<IActionResult> GetLastCommit(string repositoryId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
            throw new ValidationException("Required parameters were not provided");

        var lastCommitDate = await _repositoryHealthService.GetLastCommitDateAsync(Guid.Parse(repositoryId), ct);
        var humanReadableDate = "never";
        if (lastCommitDate.HasValue)
        {
            int daysElapsed = (DateTime.UtcNow - lastCommitDate.Value).Days;
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
        return await _badgeService.Create("last commit", humanReadableDate, color);
    }
}
