using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Models;

namespace AzureDevOps.InnerSource.Services;

public class RepositoryService
{
	private readonly RepositoryAggregator _aggregator;

	private readonly IPrincipalService _principalService;

	private readonly IStarService _starService;

	public RepositoryService(RepositoryAggregator aggregator, IStarService starService, IPrincipalService principalService)
	{
		_aggregator = aggregator;
		_starService = starService;
		_principalService = principalService;
	}

	public async Task<List<RepositoryDto>> GetRepositoriesAsync(string projectId, CancellationToken ct)
	{
		var principal = _principalService.GetPrincipal();

		var repositories = await _aggregator.GetRepositoriesAsync(projectId, ct);
		return await repositories.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(async (x, token) =>
			{
				var repository = new Repository
				{
					Organization = x.Organization,
					Project = x.Project,
					Name = x.Name
				};

				var starCount = await _starService.GetStarCountAsync(repository, token);
				var isStarred = await _starService.GetIsStarredAsync(principal, repository, token);

				return new RepositoryDto
				{
					Project = x.Project,
					Id = x.Id,
					Name = x.Name,
					Description = x.Description,
					Installation = x.Installation,
					Badges = x.Badges.Select(badge => new BadgeDto(badge.Name, badge.Url)),
					Metadata = new RepositoryMetadataDto(x.Metadata.Url, x.Metadata.LastCommitDate),
					Stars = new StarsDto(starCount, isStarred)
				};
			}).ToListAsync(ct);
	}
}