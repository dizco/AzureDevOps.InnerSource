using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.ADO.Services;

/// <summary>
///     <see cref="ProjectHttpClient" />
/// </summary>
public class ProjectAnalysisHttpClient : ProjectCompatHttpClientBase
{
	public ProjectAnalysisHttpClient(Uri baseUrl, VssCredentials credentials)
		: base(baseUrl, credentials)
	{
	}

	public ProjectAnalysisHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
		: base(baseUrl, credentials, settings)
	{
	}

	public ProjectAnalysisHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
		: base(baseUrl, credentials, handlers)
	{
	}

	public ProjectAnalysisHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
		: base(baseUrl, credentials, settings, handlers)
	{
	}

	public ProjectAnalysisHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
		: base(baseUrl, pipeline, disposeHandler)
	{
	}

	/// <remarks>
	///     Inspired by the Node implementation:
	///     https://github.com/microsoft/azure-devops-node-api/blob/55908ae28d367e7b30c167f679c4f1087f647cb2/api/ProjectAnalysisApi.ts#L36
	/// </remarks>
	public async Task<ProjectLanguageAnalytics> GetProjectLanguageAnalyticsAsync(Guid projectId, CancellationToken ct)
	{
		var locationId = new Guid("5b02a779-1867-433f-90b7-d23ed5e33e57");
		var routeValues = new { project = projectId };
		return await GetAsync<ProjectLanguageAnalytics>(locationId, routeValues, new ApiResourceVersion("7.1-preview.1"), cancellationToken: ct);
	}
}

public record LanguageStatistics
{
	public long Bytes { get; set; }

	public int Files { get; set; }

	public float FilesPercentage { get; set; }

	public string Name { get; set; }

	public float LanguagePercentage { get; set; }
}

public enum ResultPhase
{
	Preliminary = 0,
	Full = 1
}

public record RepositoryLanguageAnalytics
{
	public Guid Id { get; set; }

	public string Name { get; set; }

	public ResultPhase ResultPhase { get; set; }

	public DateTime UpdatedTime { get; set; }

	public List<LanguageStatistics> LanguageBreakdown { get; set; }
}

public record ProjectLanguageAnalytics
{
	public Guid Id { get; set; }

	public List<LanguageStatistics> LanguageBreakdown { get; set; }

	public List<RepositoryLanguageAnalytics> RepositoryLanguageAnalytics { get; set; }

	public ResultPhase ResultPhase { get; set; }

	public string Url { get; set; }
}