using AzureDevOps.InnerSource.ADO.Models;
using AzureDevOps.InnerSource.ADO.Services;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.Services;

public class RepositoryService
{
	private readonly RepositoryAggregator _aggregator;
	
	private readonly IStarService _starService;

	public RepositoryService(RepositoryAggregator aggregator, IStarService starService)
	{
		_aggregator = aggregator;
		_starService = starService;
	}

	public async Task<List<RepositoryDto>> GetRepositoriesAsync(string projectId, CancellationToken ct)
	{
		var repositories = await _aggregator.GetRepositoriesAsync(projectId, ct);
		return await repositories.ToAsyncEnumerable()
			.SelectAwaitWithCancellation(async (x, token) =>
			{
				var starCount = await _starService.GetStarCountAsync(new Repository
				{
					Organization = x.Organization,
					Project = x.Project,
					Name = x.Name,
				}, token);

				return new RepositoryDto
				{
					Project = x.Project,
					Id = x.Id,
					Name = x.Name,
					Description = x.Description,
					Installation = x.Installation,
					Badges = x.Badges.Select(badge => new BadgeDto(badge.Name, badge.Url)),
					Metadata = new RepositoryMetadataDto(x.Metadata.Url, x.Metadata.LastCommitDate),
					Stars = new StarsDto(starCount)
				};
			}).ToListAsync(ct);
	}
}