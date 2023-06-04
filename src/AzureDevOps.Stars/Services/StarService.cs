using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AzureDevOps.Stars.Configuration;
using AzureDevOps.Stars.Exceptions;
using AzureDevOps.Stars.Storage;
using Microsoft.Extensions.Options;

namespace AzureDevOps.Stars.Services;

public interface IStarService
{
	Task StarAsync(Principal principal, Repository repository);

	Task<int> GetStarCountAsync(Repository repository);
}

public class StarService : IStarService
{
	private readonly ILogger<StarService> _logger;
	
	private readonly IStarRepository _repository;

	private readonly IOptionsMonitor<DevOpsOptions> _options;

	private DevOpsOptions Options => _options.CurrentValue;

	public StarService(IStarRepository repository, IOptionsMonitor<DevOpsOptions> options, ILogger<StarService> logger)
	{
		_repository = repository;
		_options = options;
		_logger = logger;
	}

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
		       && Options.StarsAllowedRepositories.Any(x =>
			       new Regex(x.RegexProject).IsMatch(repository.Project) &&
			       new Regex(x.RegexRepository).IsMatch(repository.Name));
	}
}