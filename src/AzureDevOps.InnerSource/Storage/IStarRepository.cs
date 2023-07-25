using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Services;

namespace AzureDevOps.InnerSource.Storage;

public interface IStarRepository
{
	Task SetStarAsync(Repository repository, Principal principal, CancellationToken ct);

	Task<int> GetStarCountAsync(Repository repository, CancellationToken ct);

	Task<bool> GetIsStarredAsync(Repository repository, Principal principal, CancellationToken ct);
}