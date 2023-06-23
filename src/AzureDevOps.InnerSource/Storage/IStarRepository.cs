using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Services;

namespace AzureDevOps.InnerSource.Storage;

public interface IStarRepository
{
	Task SetStarAsync(Repository repository, Principal principal);

	Task<int> GetStarCountAsync(Repository repository);
}