using System.Collections.Concurrent;
using AzureDevOps.Stars.Services;

namespace AzureDevOps.Stars.Storage;

public class StarInMemoryRepository : IStarRepository
{
	private readonly ConcurrentDictionary<Repository, HashSet<Principal>> _stars = new();
	public Task<int> GetStarCountAsync(Repository repository)
	{
		if (_stars.TryGetValue(repository, out var users))
		{
			return Task.FromResult(users.Count);
		}

		return Task.FromResult(0);
	}

	public Task SetStarAsync(Repository repository, Principal principal)
	{
		_stars.AddOrUpdate(repository,
			new HashSet<Principal> { principal },
			(key, set) =>
			{
				set.Add(principal);
				return set;
			});

		return Task.CompletedTask;
	}
}