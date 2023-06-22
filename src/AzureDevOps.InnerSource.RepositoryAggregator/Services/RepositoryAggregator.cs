using System.Text.RegularExpressions;
using AzureDevOps.InnerSource.RepositoryAggregator.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.RepositoryAggregator.Services;

public class RepositoryAggregator
{
	// We don't explicitly dispose any Client derived from the connection because the connection handles their lifetimes
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

	private static string BuildMarkdown(List<Repository> repositories)
	{
		const string template = @"
<table class=""repositories"" width=""1000px"">
{{repositories}}
</table>
";

		const string repositoryTemplate = @"
<td style=""width: 500px"">
<h2 style=""margin: 0; margin-bottom: 5px;"">{{title}} {{language}}</h2>
<p style=""margin-bottom: 8px;"">{{description}}</p>

```shell
npm install --save package
```

<a href=""{{link}}"">Go to project</a>
</td>
";

		var repositoriesMarkdown = string.Empty;
		for (var i = 0; i < repositories.Count; i++)
		{
			if (i % 2 == 0) repositoriesMarkdown += "<tr>";

			repositoriesMarkdown += repositoryTemplate.Replace("{{title}}", repositories[i].Name)
				.Replace("{{link}}", repositories[i].WebUrl)
				.Replace("{{language}}", repositories[i].Language?.GetHtmlBadge() ?? "");

			if (i % 2 == 1) repositoriesMarkdown += "</tr>";
		}

		return template.Replace("{{repositories}}", repositoriesMarkdown);
	}

	private async Task<List<Repository>> GetRepositoriesAsync(List<Project> projects, CancellationToken ct)
	{
		var gitClient = await _connection.GetClientAsync<GitHttpClient>(ct);

		var repositories = new List<Repository>();
		foreach (var project in projects)
		{
			var projectMetrics = await GetProjectMetricsAsync(project, ct);
			var response = await gitClient.GetRepositoriesAsync(project.Id, includeHidden: false, cancellationToken: ct);
			repositories.AddRange(response
				.Where(x => !(x.IsDisabled ?? false) && !string.IsNullOrWhiteSpace(x.DefaultBranch))
				.Where(IsAllowedRepository)
				.Select(x =>
				{
					projectMetrics.TryGetValue(x.Name, out var language);
					return new Repository
					{
						Name = x.Name,
						Description = "",
						Language = language,
						WebUrl = x.WebUrl
					};
				}));
		}

		return repositories;
	}

	/// <remarks>
	///     Metrics are calculated with linguist. There are some ways to override this:
	///     https://github.com/github-linguist/linguist/blob/master/docs/overrides.md
	/// </remarks>
	private async Task<Dictionary<string, ProgrammingLanguage>> GetProjectMetricsAsync(Project project, CancellationToken ct)
	{
		var analysisClient = await _connection.GetClientAsync<ProjectAnalysisHttpClient>(ct);
		var analytics = await analysisClient.GetProjectLanguageAnalyticsAsync(project.Id, ct);

		return analytics.RepositoryLanguageAnalytics.Select(x => new { x.Name, TopLanguage = x.LanguageBreakdown.FirstOrDefault()?.Name ?? "" })
			.Where(x => !string.IsNullOrWhiteSpace(x.TopLanguage))
			.ToDictionary(x => x.Name, x => new ProgrammingLanguage(x.TopLanguage));
	}

	private async Task<List<Project>> GetProjectsAsync(CancellationToken ct)
	{
		var projectClient = await _connection.GetClientAsync<ProjectHttpClient>(ct);
		string? continuationToken = null;
		var projects = new List<Project>();
		do
		{
			var page = await projectClient.GetProjects(ProjectState.WellFormed,
				userState: null,
				continuationToken: continuationToken);
			continuationToken = page.ContinuationToken;

			var allowedProjects = page.Where(IsAllowedProject);

			projects.AddRange(allowedProjects.Select(x => new Project
			{
				Id = x.Id,
				Name = x.Name
			}));
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

internal record Project
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
}

internal class ProgrammingLanguage
{
	public ProgrammingLanguage(string name)
	{
		Name = name;
	}

	public string Name { get; }

	public string GetHtmlBadge()
	{
		return Name switch
		{
			"C#" => "<img src=\"https://img.shields.io/badge/-7.0-512BD4?logo=.net)](https://dotnet.microsoft.com/\" alt=\".NET\">",
			"TypeScript" => "<img src=\"https://img.shields.io/badge/TypeScript-007ACC?logo=typescript&logoColor=white\" alt=\"TypeScript\">",
			"JavaScript" => "<img src=\"https://img.shields.io/badge/javascript-%23323330.svg?logo=javascript&logoColor=%23F7DF1E\" alt=\"JavaScript\">",
			"C++" => "<img src=\"https://img.shields.io/badge/c++-%2300599C.svg?logo=c%2B%2B&logoColor=white\" alt=\"C++\">",
			_ => ""
		};
	}
}

internal record Repository
{
	public required string Name { get; init; }

	public required string? Description { get; init; }

	public required string WebUrl { get; init; }

	public required ProgrammingLanguage? Language { get; init; }
}
