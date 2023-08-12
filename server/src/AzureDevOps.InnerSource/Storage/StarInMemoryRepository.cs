using System.Collections.Concurrent;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Services;

namespace AzureDevOps.InnerSource.Storage;

public class StarInMemoryRepository : IStarRepository
{
	private readonly ConcurrentDictionary<Repository, HashSet<Principal>> _stars = new();
	public Task<int> GetStarCountAsync(Repository repository, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (_stars.TryGetValue(repository, out var users))
		{
			return Task.FromResult(users.Count);
		}

		return Task.FromResult(0);
	}

	public Task<bool> GetIsStarredAsync(Repository repository, Principal principal, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (_stars.TryGetValue(repository, out var users))
		{
			return Task.FromResult(users.Contains(principal));
		}
		return Task.FromResult(false);
	}

	public Task RemoveStarAsync(Repository repository, Principal principal, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (_stars.TryGetValue(repository, out var value))
		{
			value.Remove(principal);
		}

		return Task.CompletedTask;
	}

	public Task SetStarAsync(Repository repository, Principal principal, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
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