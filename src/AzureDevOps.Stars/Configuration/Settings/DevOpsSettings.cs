namespace AzureDevOps.Stars.Configuration.Settings;

internal struct DevOpsSettings
{
	public const string SectionName = "DevOps";

	public string Organization { get; init; }

	public readonly struct StarsAllowedRepositoriesSettings
	{
		public string RegexProject { get; init; }

		public string RegexRepository { get; init; }
	}

	public List<StarsAllowedRepositoriesSettings>? StarsAllowedRepositories { get; init; }
}