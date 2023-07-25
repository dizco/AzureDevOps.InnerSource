using AzureDevOps.InnerSource.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.ADO.Services;

/// <summary>
///     Provides metrics about the maintenance level of a repository
/// </summary>
public class RepositoryHealthService
{
	private readonly VssConnection _connection;

	public RepositoryHealthService(VssConnection connection)
	{
		_connection = connection;
	}

	public async Task<DateTime?> GetLastCommitDateAsync(Guid repositoryId, CancellationToken ct)
	{
		var gitClient = await _connection.GetClientAsync<GitHttpClient>(ct);
		var commits = await gitClient.GetCommitsAsync(repositoryId, new GitQueryCommitsCriteria { Top = 5 }, cancellationToken: ct);
		var lastCommit = commits.FirstOrDefault();
		return lastCommit?.Committer.Date;
	}
}