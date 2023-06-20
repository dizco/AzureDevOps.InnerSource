using System.Text.RegularExpressions;
using AzureDevOps.InnerSource.RepositoryAggregator.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.RepositoryAggregator.Services;

public class RepositoryAggregator
{
	private readonly VssConnection _connection;
	private readonly IOptionsMonitor<DevOpsOptions> _options;

	public RepositoryAggregator(VssConnection connection, IOptionsMonitor<DevOpsOptions> options)
	{
		_connection = connection;
		_options = options;
	}

	private DevOpsOptions Options => _options.CurrentValue;

	public async Task AggregateAsync()
	{
		var projects = await GetProjectsAsync();

		using var gitClient = _connection.GetClient<GitHttpClient>();

		var repositories = new List<GitRepository>();
		foreach (var project in projects)
		{
			var response = await gitClient.GetRepositoriesAsync(project.Id);
			repositories.AddRange(response.Where(x=> !(x.IsDisabled ?? false) && !string.IsNullOrWhiteSpace(x.DefaultBranch))
				.Where(IsAllowedRepository));
		}

		var t = repositories.ToList();
	}

	private async Task<List<TeamProjectReference>> GetProjectsAsync()
	{
		using var projectClient = _connection.GetClient<ProjectHttpClient>();
		string? continuationToken = null;
		var projects = new List<TeamProjectReference>();
		do
		{
			var page = await projectClient.GetProjects(ProjectState.WellFormed,
				userState: null,
				continuationToken: continuationToken);
			continuationToken = page.ContinuationToken;

			projects.AddRange(page.Where(IsAllowedProject));
		} while (continuationToken is not null);

		return projects;
	}

	private bool IsAllowedProject(TeamProjectReference project)
	{
		return Options.AllowedRepositories.Any(x => new Regex(x.RegexProject).IsMatch(project.Name));
	}

	private bool IsAllowedRepository(GitRepository repository)
	{
		return Options.AllowedRepositories.Any(x => new Regex(x.RegexProject).IsMatch(repository.ProjectReference.Name)
		                                            && new Regex(x.RegexRepository).IsMatch(repository.Name));
	}
}