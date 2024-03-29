﻿using System.Text.RegularExpressions;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Common.Configuration;
using AzureDevOps.InnerSource.Exceptions;
using AzureDevOps.InnerSource.Storage;
using Microsoft.Extensions.Options;

namespace AzureDevOps.InnerSource.Services;

public interface IStarService
{
	Task StarAsync(Principal principal, Repository repository, CancellationToken ct);

	Task<int> GetStarCountAsync(Repository repository, CancellationToken ct);

	Task<bool> GetIsStarredAsync(Principal principal, Repository repository, CancellationToken ct);
	
	Task UnstarAsync(Principal principal, Repository repository, CancellationToken ct);
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

	public async Task StarAsync(Principal principal, Repository repository, CancellationToken ct)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} does not match any allowed repository", repository);
			throw new RepositoryNotAllowedException();
		}

		await _repository.SetStarAsync(repository, principal, ct);
	}

	public async Task UnstarAsync(Principal principal, Repository repository, CancellationToken ct)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} does not match any allowed repository", repository);
			throw new RepositoryNotAllowedException();
		}

		await _repository.RemoveStarAsync(repository, principal, ct);
	}

	public async Task<int> GetStarCountAsync(Repository repository, CancellationToken ct)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} does not match any allowed repository.", repository);
			throw new RepositoryNotAllowedException();
		}

		return await _repository.GetStarCountAsync(repository, ct);
	}

	public async Task<bool> GetIsStarredAsync(Principal principal, Repository repository, CancellationToken ct)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} does not match any allowed repository.", repository);
			throw new RepositoryNotAllowedException();
		}

		return await _repository.GetIsStarredAsync(repository, principal, ct);
	}

	private bool IsAllowedRepository(Repository repository)
	{
		return string.Equals(Options.Organization, repository.Organization, StringComparison.OrdinalIgnoreCase)
		       && Options.AllowedRepositories.Any(x =>
			       new Regex(x.RegexProject).IsMatch(repository.Project) &&
			       new Regex(x.RegexRepository).IsMatch(repository.Id));
	}
}