using System.Security.Cryptography.X509Certificates;
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
<table class=""repositories"" width=""1100px"">
{{repositories}}
</table>
";

		const string repositoryTemplate = @"
<td style=""width: 550px"">
<h2 style=""margin: 0; margin-bottom: 5px;"">{{title}} {{language}}</h2>
<p style=""margin-bottom: 8px;"">{{description}}</p>

<a href=""{{link}}"">Go to project</a>
</td>
";
		/*```shell
npm install --save package
```*/

		var repositoriesMarkdown = string.Empty;
		for (var i = 0; i < repositories.Count; i++)
		{
			if (i % 2 == 0) repositoriesMarkdown += "<tr>";

			repositoriesMarkdown += repositoryTemplate.Replace("{{title}}", repositories[i].Name)
				.Replace("{{description}}", repositories[i].Description)
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
			var allowedRepositories = await response
				.Where(x => !(x.IsDisabled ?? false) && !string.IsNullOrWhiteSpace(x.DefaultBranch))
				.Where(IsAllowedRepository)
				.ToAsyncEnumerable()
				.SelectAwaitWithCancellation(async (x, token) =>
				{
					var description = await GetDescriptionAsync(x.Id, token);
					projectMetrics.TryGetValue(x.Name, out var language);
					return new Repository
					{
						Name = x.Name,
						Description = description,
						Language = language,
						WebUrl = x.WebUrl
					};
				})
				.ToListAsync(ct);

			repositories.AddRange(allowedRepositories);
		}

		return repositories;
	}

	private async Task<string> GetDescriptionAsync(Guid repositoryId, CancellationToken ct)
	{
		var readme = await GetReadmeAsync(repositoryId, ct);
		var descriptionRegex = new Regex("<p id=\"description\">(.*)<\\/p>");
		var match = descriptionRegex.Match(readme);

		if (match.Success)
		{
			var description = match.Groups[1].Value;
			// Take first 200 characters of description
			return description.Substring(0, Math.Min(description.Length, 200));
		}

		return "";
	}

	private async Task<string> GetReadmeAsync(Guid repositoryId, CancellationToken ct)
	{
		var gitClient = await _connection.GetClientAsync<GitHttpClient>(ct);
		var items = await gitClient.GetItemsAsync(repositoryId, recursionLevel: VersionControlRecursionType.OneLevel, cancellationToken: ct);
		if (items.Any(x => string.Equals(x.Path, "/readme.md", StringComparison.OrdinalIgnoreCase)))
		{
			var content = await gitClient.GetItemContentAsync(repositoryId, "readme.md", cancellationToken: ct);
			var reader = new StreamReader(content);
			return await reader.ReadToEndAsync(ct);
		}

		return string.Empty;
	} 

	/// <remarks>
	///     Metrics are calculated with linguist. There are some ways to override this:
	///     https://github.com/github-linguist/linguist/blob/master/docs/overrides.md
	/// </remarks>
	private async Task<Dictionary<string, ProgrammingLanguage>> GetProjectMetricsAsync(Project project, CancellationToken ct)
	{
		var analysisClient = await _connection.GetClientAsync<ProjectAnalysisHttpClient>(ct);
		var analytics = await analysisClient.GetProjectLanguageAnalyticsAsync(project.Id, ct);

		var repositoriesAnalytics = new Dictionary<string, ProgrammingLanguage>();
		foreach (var repository in analytics.RepositoryLanguageAnalytics)
		{
			if (!repository.LanguageBreakdown.Any())
				continue;

			var topLanguage = repository.LanguageBreakdown.MaxBy(x => x.LanguagePercentage);
			if (string.IsNullOrWhiteSpace(topLanguage?.Name))
				continue;

			repositoriesAnalytics.Add(repository.Name, new ProgrammingLanguage(topLanguage.Name));
		}

		return repositoriesAnalytics;
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
			"C#" => "<img src=\"https://img.shields.io/badge/-512BD4?logo=.net\" alt=\".NET\">",
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
