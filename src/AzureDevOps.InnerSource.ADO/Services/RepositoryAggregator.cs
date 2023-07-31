using System.Text.RegularExpressions;
using AzureDevOps.InnerSource.ADO.Configuration;
using AzureDevOps.InnerSource.ADO.Models;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.ADO.Services;

public class RepositoryAggregator
{
    // We don't explicitly dispose any Client derived from the connection because the connection handles their lifetimes
    private readonly VssConnection _connection;
    private readonly RepositoryHealthService _repositoryHealthService;
    private readonly IOptionsMonitor<DevOpsOptions> _devOpsOptions;
    private readonly IOptionsMonitor<RepositoryAggregationOptions> _options;
    private readonly IBadgeTokenService _badgeTokenService;
    private readonly ILogger<RepositoryAggregator> _logger;

    public RepositoryAggregator(VssConnection connection,
        RepositoryHealthService repositoryHealthService,
	    IOptionsMonitor<RepositoryAggregationOptions> options,
	    IOptionsMonitor<DevOpsOptions> devOpsOptions,
        IBadgeTokenService badgeTokenService,
        ILogger<RepositoryAggregator> logger)
    {
        _connection = connection;
        _repositoryHealthService = repositoryHealthService;
        _options = options;
        _devOpsOptions = devOpsOptions;
        _badgeTokenService = badgeTokenService;
        _logger = logger;
    }

    private DevOpsOptions DevOpsOptions => _devOpsOptions.CurrentValue;
    private RepositoryAggregationOptions Options => _options.CurrentValue;

    public async Task AggregateAsync(CancellationToken ct)
    {
        _logger.LogInformation("Start repositories aggregation");
        var projects = await GetProjectsAsync(ct);
        var repositories = await GetRepositoriesAsync(projects, ct);

        var filePath = Options.OutputFolder + "/result.md";
        Directory.CreateDirectory(Options.OutputFolder);
        var md = BuildMarkdown(repositories);
        await File.WriteAllTextAsync(filePath, md, ct);
        _logger.LogInformation("Finished aggregating {count} repositories", repositories.Count);
    }

    private string BuildMarkdown(List<AdoRepository> repositories)
    {
        const string template = @"<table id=""repositoriesAggregation"" width=""900px"">
{{repositories}}
</table>
";

        const string repositoryTemplate = @"
<td style=""width: 450px"">
<h2 style=""margin: 0; margin-bottom: 5px;"">{{title}}</h2>
<p style=""margin-bottom: 5px;""><img src=""{{badgeServerUrl}}/{{adoProject}}/repositories/{{repositoryId}}/badges/stars?access_token={{badgeJwt}}"" alt=""Stars""> <img src=""{{badgeServerUrl}}/{{adoProject}}/repositories/{{repositoryId}}/badges/last-commit?access_token={{badgeJwt}}"" alt=""Last commit""> {{language}}</p>
<p style=""margin-bottom: 8px;"">{{description}}</p>
<pre><code>{{installation}}</code></pre>
<a href=""{{link}}"">Go to adoProject</a>
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

            var notBefore = DateTime.UtcNow;
            var expires = DateTime.UtcNow.AddDays(5);
            var badgeJwt = _badgeTokenService.GenerateBadgeJwt(repositories[i].Project, repositories[i].Id.ToString(), notBefore, expires);

			repositoriesMarkdown += repositoryTemplate.Replace("{{title}}", repositories[i].Name)
                .Replace("{{repositoryId}}", repositories[i].Id.ToString())
                .Replace("{{adoProject}}", repositories[i].Project)
                .Replace("{{description}}", repositories[i].Description)
                .Replace("{{installation}}", repositories[i].Installation)
                .Replace("{{link}}", repositories[i].Metadata.Url)
                .Replace("{{language}}",  languageBadge)
                .Replace("{{badgeServerUrl}}", Options.BadgeServerUrl)
                .Replace("{{badgeJwt}}", badgeJwt);

            if (i % 2 == 1) repositoriesMarkdown += "</tr>";
        }

        return template.Replace("{{repositories}}", repositoriesMarkdown);
    }

    public async Task<List<AdoRepository>> GetRepositoriesAsync(string projectId, CancellationToken ct)
    {
	    var projectClient = await _connection.GetClientAsync<ProjectHttpClient>(ct);
	    var project = await projectClient.GetProject(projectId);

        return await GetRepositoriesAsync(new List<AdoProject>
        {
	        new() { Id = project.Id, Name = project.Name }
        }, ct);
	}


	private async Task<List<AdoRepository>> GetRepositoriesAsync(List<AdoProject> projects, CancellationToken ct)
    {
        var gitClient = await _connection.GetClientAsync<GitHttpClient>(ct);

        var repositories = new List<AdoRepository>();
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
                    var readme = await GetReadmeAsync(x.Id, token);
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
	                    new("Stars", $"{Options.BadgeServerUrl}/{project.Name}/repositories/{x.Id}/badges/stars", true),
	                    new("Last Commit", $"{Options.BadgeServerUrl}/{project.Name}/repositories/{x.Id}/badges/last-commit", true)
                    };
                    if (language is not null)
                    {
                        badges.Add(new Badge(language.Name, language.GetBadgeUrl()));
                    }

                    var lastCommitDate = await _repositoryHealthService.GetLastCommitDateAsync(x.Id, token);

					return new AdoRepository
                    {
                        Organization = DevOpsOptions.Organization,
                        Project = project.Name,
                        Name = x.Name,
                        Id = x.Id,
                        Description = description,
                        Installation = installation,
                        Language = language,
                        Badges = badges,
                        Metadata = new AdoRepositoryMetadata
                        {
	                        Url = x.WebUrl,
                            LastCommitDate = lastCommitDate
						}
                    };
                })
                .ToListAsync(ct);

            repositories.AddRange(allowedRepositories);
        }

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
    private async Task<Dictionary<string, ProgrammingLanguage>> GetProjectMetricsAsync(AdoProject adoProject, CancellationToken ct)
    {
        var analysisClient = await _connection.GetClientAsync<ProjectAnalysisHttpClient>(ct);
        var analytics = await analysisClient.GetProjectLanguageAnalyticsAsync(adoProject.Id, ct);

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

    private async Task<List<AdoProject>> GetProjectsAsync(CancellationToken ct)
    {
        var projectClient = await _connection.GetClientAsync<ProjectHttpClient>(ct);
        string? continuationToken = null;
        var projects = new List<AdoProject>();
        do
        {
            var page = await projectClient.GetProjects(ProjectState.WellFormed,
                userState: null,
                continuationToken: continuationToken);
            continuationToken = page.ContinuationToken;

            var allowedProjects = page.Where(IsAllowedProject);

            projects.AddRange(allowedProjects.Select(x => new AdoProject
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