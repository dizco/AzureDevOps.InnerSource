namespace AzureDevOps.InnerSource.Configuration.Settings;

internal struct StorageSettings
{
	public const string SectionName = "Storage";

	public string Mode { get; init; }

	public string TableStorageConnectionString { get; init; }

	public string TableName { get; init; }
}