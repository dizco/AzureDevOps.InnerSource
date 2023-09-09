using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.InnerSource.Controllers;

public class TokenController : Controller
{
	private readonly ILogger<TokenController> _logger;

	private readonly ITokenService _tokenService;

	public TokenController(ITokenService tokenService, ILogger<TokenController> logger)
	{
		_tokenService = tokenService;
		_logger = logger;
	}

	[HttpPost("token")]
	[EnableCors("AzureDevOpsExtension")]
	public async Task<IActionResult> PostToken(CancellationToken ct)
	{
		var challengeResult = await HttpContext.AuthenticateAsync("AzureDevOpsExtension");
		if (challengeResult.Succeeded)
		{
			if (HttpContext.Request.Headers.TryGetValue("X-AzureDevOps-AccessToken", out var userAccessToken))
			{
				var devOpsResult = await _tokenService.ParseAzureDevOpsTokensAsync(challengeResult.Principal, userAccessToken.ToString(), ct);
				if (devOpsResult.IsAuthenticated)
				{
					var jwtToken = _tokenService.GenerateJwt(devOpsResult.Claims, devOpsResult.NotBefore, devOpsResult.Expires);
					var expiresInSeconds = Math.Floor((devOpsResult.Expires - DateTime.UtcNow).TotalSeconds);
					if (expiresInSeconds <= 0) throw new Exception("Expected the expiration of the token to be in the future, but it is already expired");

					return Json(new
					{
						accessToken = jwtToken,
						expiresInSeconds
					});
				}

				_logger.LogError("Could not parse Azure DevOps tokens");
			}
			else
			{
				_logger.LogError("User access token not found in request headers");
			}
		}
		else
		{
			_logger.LogError("Could not authenticate the Azure DevOps access token");
		}
		

		return Unauthorized();
	}

	[Authorize]
	[HttpPost("{projectName}/repositories/{repositoryId}/badges/token")]
	[EnableCors("AzureDevOpsExtension")]
	public IActionResult PostBadgeToken(string projectName, string repositoryId)
	{
		var notBefore = DateTime.UtcNow;
		var expires = DateTime.UtcNow.AddDays(30); // TODO: Could probably extend this
		var jwtToken = _tokenService.GenerateBadgeJwt(projectName, repositoryId, notBefore, expires);
		var expiresInSeconds = Math.Floor((expires - notBefore).TotalSeconds);

		return Json(new
		{
			accessToken = jwtToken,
			expiresInSeconds
		});
	}
}