using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Controllers;

public class StarsController : Controller
{
    private readonly IOptionsMonitor<DevOpsOptions> _options;
    private readonly IStarService _starService;
    private readonly BadgeService _badgeService;
    private readonly ILogger<StarsController> _logger;

    public StarsController(IStarService starService, BadgeService badgeService, IOptionsMonitor<DevOpsOptions> options, ILogger<StarsController> logger)
    {
        _starService = starService;
        _badgeService = badgeService;
        _options = options;
        _logger = logger;
    }

    private DevOpsOptions Options => _options.CurrentValue;

    [Authorize]
    [EnableCors("AzureDevOpsExtension")]
    [HttpPost("{projectName}/repositories/{repositoryId}/stars")]
    public async Task<IActionResult> PostStar(string projectName, string repositoryId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(repositoryId))
            throw new ValidationException("Required parameters were not provided");

        var principal = new Principal
        {
            Id = User.FindFirstValue("sub") ?? throw new Exception("Expected to find a sub claim"),
            Email = User.FindFirstValue("email")
        };

        await _starService.StarAsync(principal, new Repository
        {
            Organization = Options.Organization,
            Project = projectName,
            Id = repositoryId
		}, ct);
        
        return Json(new {});
    }

	[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},AzureDevOpsBadge")]
	[HttpGet("{projectName}/repositories/{repositoryId}/badges/stars")]
	[EnableCors("AzureDevOpsExtension")]
	public async Task<IActionResult> GetStarsBadge(string projectName, string repositoryId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(repositoryId))
            throw new ValidationException("Required parameters were not provided");

        if (!User.HasClaim(x => string.Equals(x.Type, "sub", StringComparison.OrdinalIgnoreCase))
            && !User.HasClaim("repositoryId", repositoryId))
        {
	        _logger.LogError("Badge access token is not permitted for the requested repository {repositoryId}", repositoryId);
	        return Forbid();
        }

		var repository = new Repository
        {
            Organization = Options.Organization,
            Project = projectName,
            Id = repositoryId
		};
        var stars = await _starService.GetStarCountAsync(repository, ct);

        // TODO: Read this once and cache it to avoid unecessary IO operations
        byte[] imageArray = await System.IO.File.ReadAllBytesAsync(@"Assets/segoe-star.png", ct);
        string base64ImageRepresentation = Convert.ToBase64String(imageArray);

        return await _badgeService.CreateAsync("stars", stars.ToString(), "informational",
            logo: $"data:image/png;base64,{base64ImageRepresentation}",
            logoColor: "yellow",
            ct: ct);
    }
}