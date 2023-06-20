using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.InnerSource.RepositoryAggregator.Configuration;

public class DevOpsOptions
{
	[Required]
	public string Organization { get; set; } = null!;

	[Required]
	public string PersonalAccessToken { get; set; } = null!;

	[Required]
	public List<(string RegexProject, string RegexRepository)> AllowedRepositories { get; set; } = null!;
}