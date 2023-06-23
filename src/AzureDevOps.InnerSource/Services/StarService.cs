using System.Text.RegularExpressions;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Exceptions;
using AzureDevOps.InnerSource.Storage;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Services;

public interface IStarService
{
	Task StarAsync(Principal principal, Repository repository);

	Task<int> GetStarCountAsync(Repository repository);
}

public class StarService : IStarService
{
	private readonly ILogger<StarService> _logger;

	private readonly IOptionsMonitor<DevOpsOptions> _options;

	private readonly IStarRepository _repository;

	public StarService(IStarRepository repository, IOptionsMonitor<DevOpsOptions> options, ILogger<StarService> logger)
	{
		_repository = repository;
		_options = options;
		_logger = logger;
	}

	private DevOpsOptions Options => _options.CurrentValue;

	public async Task StarAsync(Principal principal, Repository repository)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} does not match any allowed repository", repository);
			throw new RepositoryNotAllowedException();
		}

		await _repository.SetStarAsync(repository, principal);
	}

	public async Task<int> GetStarCountAsync(Repository repository)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} does not match any allowed repository.", repository);
			throw new RepositoryNotAllowedException();
		}

		return await _repository.GetStarCountAsync(repository);
	}

	private bool IsAllowedRepository(Repository repository)
	{
		return string.Equals(Options.Organization, repository.Organization, StringComparison.OrdinalIgnoreCase)
		       && Options.AllowedRepositories.Any(x =>
			       new Regex(x.RegexProject).IsMatch(repository.Project) &&
			       new Regex(x.RegexRepository).IsMatch(repository.Name));
	}
}