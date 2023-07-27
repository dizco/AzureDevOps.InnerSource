using System.Security.Claims;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AzureDevOps.InnerSource.Services;

public class RepositoryService
{
	private const string CacheKey = nameof(RepositoryService);

	private readonly IMemoryCache _cache;

	private readonly RepositoryAggregator _aggregator;

	private readonly IPrincipalService _principalService;

	private readonly IStarService _starService;

	private readonly ITokenService _tokenService;

	public RepositoryService(IMemoryCache cache,
		RepositoryAggregator aggregator,
		IStarService starService,
		IPrincipalService principalService,
		ITokenService tokenService)
	{
		_cache = cache;
		_aggregator = aggregator;
		_starService = starService;
		_principalService = principalService;
		_tokenService = tokenService;
	}

	public async Task<List<RepositoryDto>> GetRepositoriesAsync(string projectId, CancellationToken ct)
	{
		if (_cache.TryGetValue(CacheKey, out List<RepositoryDto>? list) && list is not null)
		{
			return list;
		}

		var principal = _principalService.GetPrincipal();

		var repositories = await _aggregator.GetRepositoriesAsync(projectId, ct);
		list = await repositories.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(async (x, token) =>
			{
				var repository = new Repository
				{
					Organization = x.Organization,
					Project = x.Project,
					Id = x.Id.ToString(),
				};

				var starCount = await _starService.GetStarCountAsync(repository, token);
				var isStarred = await _starService.GetIsStarredAsync(principal, repository, token);

				var notBefore = DateTime.UtcNow;
				var expires = DateTime.UtcNow.AddHours(1);
				var jwtToken = _tokenService.GenerateJwt(new Claim[]
				{
					new("project", x.Project),
					new("repositoryId", x.Id.ToString()),
					// TODO: Claim for the organization?
					new("scope", "badges.read")
				}, notBefore, expires);

				return new RepositoryDto
				{
					Project = x.Project,
					Id = x.Id,
					Name = x.Name,
					Description = x.Description,
					Installation = x.Installation,
					Badges = x.Badges.Select(badge =>
					{
						var url = badge.Url;
						if (badge.RequiresAuthentication)
						{
							url += "?access_token=" + jwtToken;
						}
						return new BadgeDto(badge.Name, url);
					}),
					Metadata = new RepositoryMetadataDto(x.Metadata.Url, x.Metadata.LastCommitDate),
					Stars = new StarsDto(starCount, isStarred)
				};
			}).ToListAsync(ct);
		
		_cache.Set(CacheKey, list, TimeSpan.FromSeconds(30));

		return list;
	}
}