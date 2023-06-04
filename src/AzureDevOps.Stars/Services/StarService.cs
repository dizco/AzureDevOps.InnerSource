using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AzureDevOps.Stars.Configuration;
using Microsoft.Extensions.Options;

namespace AzureDevOps.Stars.Services;

public interface IStarService
{
	Task StarAsync(Principal principal, Repository repository);

	Task<HashSet<Principal>> GetStarsAsync(Repository repository);
}

public class StarService : IStarService
{
	private readonly ILogger<StarService> _logger;
	private readonly ConcurrentDictionary<Repository, HashSet<Principal>> _stars = new();

	private readonly IOptionsMonitor<DevOpsOptions> _options;

	private DevOpsOptions Options => _options.CurrentValue;

	public StarService(IOptionsMonitor<DevOpsOptions> options, ILogger<StarService> logger)
	{
		_options = options;
		_logger = logger;
	}

	public Task StarAsync(Principal principal, Repository repository)
	{
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} is not allowed.", repository);
			return Task.CompletedTask;
		}

		// TODO: This probably doesn't perform very well in concurrent scenarios, but its fine for proof of concept
		_stars.AddOrUpdate(repository,
			new HashSet<Principal> { principal },
			(key, set) =>
			{
				set.Add(principal);
				return set;
			});

		return Task.CompletedTask;
	}

	public Task<HashSet<Principal>> GetStarsAsync(Repository repository)
	{
		var emptyResult = new HashSet<Principal>(0);
		if (!IsAllowedRepository(repository))
		{
			_logger.LogInformation("Repository {repository} is not allowed.", repository);
			return Task.FromResult(emptyResult);
		}

		if (_stars.TryGetValue(repository, out var users)) return Task.FromResult(users);

		return Task.FromResult(emptyResult);
	}

	private bool IsAllowedRepository(Repository repository)
	{
		return string.Equals(Options.Organization, repository.Organization, StringComparison.OrdinalIgnoreCase)
		       && Options.StarsAllowedRepositories.Any(x =>
			       new Regex(x.RegexProject).IsMatch(repository.Project) &&
			       new Regex(x.RegexRepository).IsMatch(repository.Name));
	}
}