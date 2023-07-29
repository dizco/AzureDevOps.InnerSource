namespace AzureDevOps.InnerSource.Configuration.Settings;

internal struct AuthenticationSettings
{
	public const string SectionName = "Authentication";

	public string Key { get; init; }

	public string Issuer { get; init; }

	public string Audience { get; init; }

	public string AzureDevOpsKey { get; init; }
}