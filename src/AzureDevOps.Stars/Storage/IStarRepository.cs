using AzureDevOps.Stars.Services;

namespace AzureDevOps.Stars.Storage;

public interface IStarRepository
{
	Task SetStarAsync(Repository repository, Principal principal);

	Task<int> GetStarCountAsync(Repository repository);
}