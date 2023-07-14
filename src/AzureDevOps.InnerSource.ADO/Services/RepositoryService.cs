using AzureDevOps.InnerSource.ADO.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.InnerSource.ADO.Services;

public class RepositoryService
{
	private readonly RepositoryAggregator _aggregator;
	private readonly VssConnection _connection;

	public RepositoryService(VssConnection connection, RepositoryAggregator aggregator)
	{
		_connection = connection;
		_aggregator = aggregator;
	}

	public async Task<List<Repository>> GetRepositoriesAsync(string projectId, CancellationToken ct)
	{
		var projectClient = await _connection.GetClientAsync<ProjectHttpClient>(ct);
		var project = await projectClient.GetProject(projectId);

		var repositories = await _aggregator.GetRepositoriesAsync(new List<Project>
		{
			new() { Id = project.Id, Name = project.Name }
		}, ct);

		return repositories;
	}
}