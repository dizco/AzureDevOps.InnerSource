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
	// We don't explicitly dispose any Client derived from the connection because the connection handles their lifetimes
	private readonly VssConnection _connection;
	private readonly IOptionsMonitor<DevOpsOptions> _devOpsOptions;

	public RepositoryHealthService(VssConnection connection, IOptionsMonitor<DevOpsOptions> devOpsOptions)
	{
		_connection = connection;
		_devOpsOptions = devOpsOptions;
	}

	public async Task<DateTime?> GetLastCommitDateAsync(Guid repositoryId, CancellationToken ct)
	{
		var gitClient = await _connection.GetClientAsync<GitHttpClient>(ct);
		var commits = await gitClient.GetCommitsAsync(repositoryId, new GitQueryCommitsCriteria { Top = 5 }, cancellationToken: ct);
		var lastCommit = commits.FirstOrDefault();
		return lastCommit?.Committer.Date;
	}
}