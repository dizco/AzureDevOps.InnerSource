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

	public async Task AggregateAsync(CancellationToken ct)
	{
		var projects = await GetProjectsAsync(ct);
		var repositories = await GetRepositoriesAsync(projects, ct);

		var md = BuildMarkdown(repositories);
		await File.WriteAllTextAsync("result.md", md, ct);
	}

	private static string BuildMarkdown(List<GitRepository> repositories)
	{
		const string template = @"
<table class=""repositories"" width=""900px"">
{{repositories}}
</table>
";

		const string repositoryTemplate = @"
<td style=""width: 450px"">
<h2 style=""margin: 0; margin-bottom: 5px;"">{{title}}</h2>
<p style=""margin-bottom: 8px;"">{{description}}</p>

```c++
int foo() {
    int result = 4;
    return result;
}
```

<a href=""{{link}}"">Go to project</a>
</td>
";

		var repositoriesMarkdown = string.Empty;
		for (var i = 0; i < repositories.Count; i++)
		{
			if (i % 2 == 0) repositoriesMarkdown += "<tr>";

			repositoriesMarkdown += repositoryTemplate.Replace("{{title}}", repositories[i].Name)
				.Replace("{{link}}", repositories[i].WebUrl);

			if (i % 2 == 1) repositoriesMarkdown += "</tr>";
		}

		return template.Replace("{{repositories}}", repositoriesMarkdown);
	}

	private async Task<List<GitRepository>> GetRepositoriesAsync(List<TeamProjectReference> projects,
		CancellationToken ct)
	{
		using var gitClient = _connection.GetClient<GitHttpClient>();

		var repositories = new List<GitRepository>();
		foreach (var project in projects)
		{
			var response =
				await gitClient.GetRepositoriesAsync(project.Id, includeHidden: false, cancellationToken: ct);
			repositories.AddRange(response
				.Where(x => !(x.IsDisabled ?? false) && !string.IsNullOrWhiteSpace(x.DefaultBranch))
				.Where(IsAllowedRepository));
		}

		return repositories;
	}

	private async Task<List<TeamProjectReference>> GetProjectsAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

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
		} while (continuationToken is not null && !ct.IsCancellationRequested);

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