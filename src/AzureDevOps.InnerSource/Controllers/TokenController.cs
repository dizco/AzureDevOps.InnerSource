﻿using AzureDevOps.InnerSource.Services;
using Microsoft.AspNetCore.Authentication;
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
	public async Task<IActionResult> Authenticate(CancellationToken ct)
	{
		var challengeResult = await HttpContext.AuthenticateAsync("AzureDevOpsExtension");
		if (challengeResult.Succeeded && HttpContext.Request.Headers.TryGetValue("X-AzureDevOps-AccessToken", out var userAccessToken))
		{
			var devOpsResult = await _tokenService.ParseAzureDevOpsTokensAsync(challengeResult.Principal, userAccessToken.ToString(), ct);
			if (devOpsResult.IsAuthenticated)
			{
				var jwtToken = _tokenService.GenerateJwt(devOpsResult.Claims, devOpsResult.NotBefore, devOpsResult.Expires);
				var expiresInSeconds = (DateTime.UtcNow - devOpsResult.Expires).Seconds;
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

		return Unauthorized();
	}
}