using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Common.Services;
using AzureDevOps.InnerSource.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.Services;

public interface ITokenService : IBadgeTokenService
{
	Task<AzureDevOpsAuthenticationResult> ParseAzureDevOpsTokensAsync(ClaimsPrincipal principal, string userAccessToken, CancellationToken ct);

	string GenerateJwt(Claim[] claims, DateTime notBefore, DateTime expires);
}

public record AzureDevOpsAuthenticationResult
{
	[MemberNotNullWhen(true, nameof(Claims), nameof(NotBefore), nameof(Expires))]
	public bool IsAuthenticated { get; init; }

	public Claim[]? Claims { get; init; }

	public DateTime NotBefore { get; init; }

	public DateTime Expires { get; init; }
}

public class TokenService : ITokenService
{
	private readonly IOptionsMonitor<AuthenticationOptions> _authenticationOptions;

	private readonly IOptionsMonitor<DevOpsOptions> _devOpsOptions;

	private readonly ILogger<TokenService> _logger;

	public TokenService(IOptionsMonitor<DevOpsOptions> devOpsOptions,
		IOptionsMonitor<AuthenticationOptions> authenticationOptions,
		ILogger<TokenService> logger)
	{
		_devOpsOptions = devOpsOptions;
		_authenticationOptions = authenticationOptions;
		_logger = logger;
	}

	private AuthenticationOptions AuthenticationOptions => _authenticationOptions.CurrentValue;

	private DevOpsOptions DevOpsOptions => _devOpsOptions.CurrentValue;

	public async Task<AzureDevOpsAuthenticationResult> ParseAzureDevOpsTokensAsync(ClaimsPrincipal principal, string userAccessToken, CancellationToken ct)
	{
		var connection = await ConnectToVssAsync(userAccessToken, ct);
		if (string.Equals(connection.AuthenticatedIdentity.Descriptor.IdentityType, IdentityConstants.System_PublicAccess, StringComparison.Ordinal))
		{
			_logger.LogError("Access token is invalid, public access is unauthorized");
			return new AzureDevOpsAuthenticationResult
			{
				IsAuthenticated = false
			};
		}

		// No need to actually validate any of the access token properties because this is done by the Vss Connection above
		var tokenHandler = new JwtSecurityTokenHandler();
		var token = tokenHandler.ReadJwtToken(userAccessToken);
		var accessTokenNameId = token.Claims.FirstOrDefault(x => x.Type == "nameid")?.Value;

		var nameid = principal.FindFirstValue("nameid");
		if (!string.Equals(nameid, accessTokenNameId, StringComparison.Ordinal))
		{
			_logger.LogError("Bearer token identity {bearerIdentity} does not match access token identity {accessTokenIdentity}", nameid,
				accessTokenNameId);
			return new AzureDevOpsAuthenticationResult
			{
				IsAuthenticated = false
			};
		}

		var userId = connection.AuthenticatedIdentity.Id;
		var email = connection.AuthenticatedIdentity.GetProperty("Account", "");
		_logger.LogInformation("Authenticated user {@data}", new
		{
			userId,
			email
		});

		var claims = new[]
		{
			new Claim("sub", DevOpsOptions.Organization + "|" + userId)
		};
		if (!string.IsNullOrEmpty(email))
		{
			claims = claims.Append(new Claim("email", email)).ToArray();
		}

		return new AzureDevOpsAuthenticationResult
		{
			IsAuthenticated = true,
			Claims = claims,
			NotBefore = token.ValidFrom,
			Expires = token.ValidTo
		};
	}

	public string GenerateJwt(Claim[] claims, DateTime notBefore, DateTime expires)
	{
		// See: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-7.0#create-an-authentication-cookie
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthenticationOptions.Key));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(AuthenticationOptions.Issuer,
			AuthenticationOptions.Audience,
			claims,
			expires: expires,
			notBefore: notBefore,
			signingCredentials: credentials);

		var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

		return jwtToken;
	}

	public string GenerateBadgeJwt(string projectName, string repositoryId, DateTime notBefore, DateTime expires)
	{
		// TODO: Should we have a claim for the organization?
		var claims = new Claim[]
		{
			new("project", projectName),
			new("repositoryId", repositoryId)
		};
		return GenerateJwt(claims, notBefore, expires);
	}

	private async Task<VssConnection> ConnectToVssAsync(string userAccessToken, CancellationToken ct)
	{
		var credential = new VssOAuthAccessTokenCredential(userAccessToken);
		var connection = new VssConnection(new Uri($"https://dev.azure.com/{DevOpsOptions.Organization}"), credential);
		await connection.ConnectAsync(ct); // This essentially calls https://dev.azure.com/{organization}/_apis/connectionData

		return connection;
	}
}