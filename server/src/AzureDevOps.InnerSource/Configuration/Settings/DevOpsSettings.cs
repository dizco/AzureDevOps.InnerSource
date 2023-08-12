namespace AzureDevOps.InnerSource.Configuration.Settings;

internal struct DevOpsSettings
{
	public const string SectionName = "DevOps";

	public string Organization { get; init; }

	public string PersonalAccessToken { get; init; }

	public readonly struct AllowedRepositoriesSettings
	{
		public string RegexProject { get; init; }

		public string RegexRepository { get; init; }
	}

	public List<AllowedRepositoriesSettings>? AllowedRepositories { get; init; }
}