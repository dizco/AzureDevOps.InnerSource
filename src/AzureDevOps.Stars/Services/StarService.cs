using System.Collections.Concurrent;
using System.Security.Claims;

namespace AzureDevOps.Stars.Services;

public record Repository
{
	public required string Name { get; init; }
	public required string Project { get; init; }
	public required string Organization { get; init; }
}

public record Principal
{
	public required string Oid { get; init; }
}

public interface IStarService
{
	Task Star(ClaimsPrincipal principal, Repository repository);

	Task<HashSet<Principal>> GetStars(Repository repository);
}

public class StarService : IStarService
{
	private readonly ConcurrentDictionary<Repository, HashSet<Principal>> _stars = new();

	public Task Star(ClaimsPrincipal principal, Repository repository)
	{
		var user = new Principal
		{
			Oid = principal.FindFirstValue("oid") ?? throw new Exception("Expected to find an oid claim")
		};

		// TODO: This probably doesn't perform very well in concurrent scenarios, but its fine for proof of concept
		_stars.AddOrUpdate(repository,
			new HashSet<Principal> { user },
			(key, set) =>
			{
				set.Add(user);
				return set;
			});

		return Task.CompletedTask;
	}

	public Task<HashSet<Principal>> GetStars(Repository repository)
	{
		if (_stars.TryGetValue(repository, out var users)) return Task.FromResult(users);

		return Task.FromResult(new HashSet<Principal>(0));
	}
}