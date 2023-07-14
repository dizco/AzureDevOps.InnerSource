using System.Text.RegularExpressions;
using AzureDevOps.InnerSource.ADO.Configuration;
using AzureDevOps.InnerSource.ADO.Models;
using AzureDevOps.InnerSource.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.ADO.Services;

public class RepositoryAggregator
{
    // We don't explicitly dispose any Client derived from the connection because the connection handles their lifetimes
    private readonly VssConnection _connection;
    private readonly IOptionsMonitor<DevOpsOptions> _devOpsOptions;
    private readonly IOptionsMonitor<RepositoryAggregationOptions> _options;

    public RepositoryAggregator(VssConnection connection, IOptionsMonitor<RepositoryAggregationOptions> options, IOptionsMonitor<DevOpsOptions> devOpsOptions)
    {
        _connection = connection;
        _options = options;
        _devOpsOptions = devOpsOptions;
    }

    private DevOpsOptions DevOpsOptions => _devOpsOptions.CurrentValue;
    private RepositoryAggregationOptions Options => _options.CurrentValue;

    public async Task AggregateAsync(CancellationToken ct)
    {
        var projects = await GetProjectsAsync(ct);
        var repositories = await GetRepositoriesAsync(projects, ct);

        var filePath = Options.OutputFolder + "/result.md";
        Directory.CreateDirectory(Options.OutputFolder);
        var md = BuildMarkdown(repositories);
        await File.WriteAllTextAsync(filePath, md, ct);
    }

    private string BuildMarkdown(List<Repository> repositories)
    {
        const string template = @"<table id=""repositoriesAggregation"" width=""900px"">
{{repositories}}
</table>
";

        const string repositoryTemplate = @"
<td style=""width: 450px"">
<h2 style=""margin: 0; margin-bottom: 5px;"">{{title}}</h2>
<p style=""margin-bottom: 5px;""><img src=""{{badgeServerUrl}}/stars/{{project}}/{{repository}}"" alt=""Stars""> <img src=""{{badgeServerUrl}}/badges/last-commit/{{repositoryId}}"" alt=""Last commit""> {{language}}</p>
<p style=""margin-bottom: 8px;"">{{description}}</p>
<pre><code>{{installation}}</code></pre>
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

            var languageBadge = repositories[i].Language?.GetBadgeUrl() ?? "";
            if (!string.IsNullOrEmpty(languageBadge))
            {
	            languageBadge = $"<img src=\"{languageBadge}\" alt=\"{repositories[i].Language!.Name}\">";

            }
            repositoriesMarkdown += repositoryTemplate.Replace("{{title}}", repositories[i].Name)
                .Replace("{{repositoryId}}", repositories[i].Id.ToString())
                .Replace("{{repository}}", repositories[i].Name)
                .Replace("{{project}}", repositories[i].Project)
                .Replace("{{description}}", repositories[i].Description)
                .Replace("{{installation}}", repositories[i].Installation)
                .Replace("{{link}}", repositories[i].WebUrl)
                .Replace("{{language}}",  languageBadge)
                .Replace("{{badgeServerUrl}}", Options.BadgeServerUrl);

            if (i % 2 == 1) repositoriesMarkdown += "</tr>";
        }

        return template.Replace("{{repositories}}", repositoriesMarkdown);
    }

    public async Task<List<Repository>> GetRepositoriesAsync(List<Project> projects, CancellationToken ct)
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
                    var readme = await GetReadmeAsync(x.Id, ct);
                    string description;
                    string installation;
                    if (Options.Overrides.TryGetValue($"{project.Name}/{x.Name}", out var o))
                    {
                        description = o.Description;
                        installation = o.Installation;
                    }
                    else
                    {
                        description = GetDescription(readme);
                        installation = GetInstallation(readme);
                    }

                    projectMetrics.TryGetValue(x.Name, out var language);

                    var badges = new List<Badge>
                    {
	                    new("Stars", $"{Options.BadgeServerUrl}/stars/{project.Name}/{x.Name}"),
	                    new("Last Commit", $"{Options.BadgeServerUrl}/badges/last-commit/{x.Id}"),
                    };
                    if (language is not null)
                    {
                        badges.Add(new Badge(language.Name, language.GetBadgeUrl()));
                    }

					return new Repository
                    {
                        Project = project.Name,
                        Name = x.Name,
                        Id = x.Id,
                        Description = description,
                        Installation = installation,
                        Language = language,
                        WebUrl = x.WebUrl,
                        Badges = badges
                    };
                })
                .ToListAsync(ct);

            repositories.AddRange(allowedRepositories);
        }

        // TODO: Query number of stars for each repo
        // TODO: Sort repositories by number of stars or by last commit

        return repositories;
    }

    private static string GetDescription(string readme)
    {
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

    private static string GetInstallation(string readme)
    {
        var installationRegex = new Regex("<pre id=\"packageInstallation\"><code>(.*)</code><\\/pre>");
        var match = installationRegex.Match(readme);

        if (match.Success)
        {
            var installation = match.Groups[1].Value;
            // Take first 400 characters of installation
            return installation.Substring(0, Math.Min(installation.Length, 400));
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
        return DevOpsOptions.AllowedRepositories.Any(x => new Regex(x.RegexProject).IsMatch(project.Name));
    }

    private bool IsAllowedRepository(GitRepository repository)
    {
        return DevOpsOptions.AllowedRepositories.Any(x => new Regex(x.RegexProject).IsMatch(repository.ProjectReference.Name)
                                                          && new Regex(x.RegexRepository).IsMatch(repository.Name));
    }
}