namespace AzureDevOps.Stars.Configuration.Settings;

internal struct IdentityProviderSettings
{
	public const string SectionName = "IdentityProvider";

	public string Authority { get; init; }

	public string ClientId { get; init; }

	public string ClientSecret { get; init; }
}