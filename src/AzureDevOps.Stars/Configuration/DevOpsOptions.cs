using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.Stars.Configuration;

public class DevOpsOptions
{
	[Required]
	public string Organization { get; set; } = null!;

	[Required]
	public string PersonalAccessToken { get; set; } = null!;

	[Required]
	public List<(string RegexProject, string RegexRepository)> StarsAllowedRepositories { get; set; } = null!;
}