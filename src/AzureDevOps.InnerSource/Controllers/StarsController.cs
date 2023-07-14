using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Controllers;

[Route("stars")]
public class StarsController : Controller
{
    private readonly IOptionsMonitor<DevOpsOptions> _options;
    private readonly IStarService _starService;
    private readonly BadgeService _badgeService;

    public StarsController(IStarService starService, BadgeService badgeService, IOptionsMonitor<DevOpsOptions> options)
    {
        _starService = starService;
        _badgeService = badgeService;
        _options = options;
    }

    private DevOpsOptions Options => _options.CurrentValue;

    [Authorize]
    [HttpPost("{project}/{repository}")]
    public async Task<IActionResult> Star(string project, string repository)
    {
        if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(repository))
            throw new ValidationException("Required parameters were not provided");

        var principal = new Principal
        {
            Id = User.FindFirstValue("ado-userid") ?? throw new Exception("Expected to find an ado-userid claim"),
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

    // TODO: Think about how to authenticate this
    [HttpGet("stars/{project}/{repositoryName}")]
    public async Task<IActionResult> GetStars(string project, string repositoryName, CancellationToken ct)
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

        // TODO: Read this once and cache it to avoid unecessary IO operations
        byte[] imageArray = await System.IO.File.ReadAllBytesAsync(@"Assets/segoe-star.png", ct);
        string base64ImageRepresentation = Convert.ToBase64String(imageArray);

        return await _badgeService.CreateAsync("stars", stars.ToString(), "informational",
            logo: $"data:image/png;base64,{base64ImageRepresentation}",
            logoColor: "yellow",
            ct: ct);
    }
}