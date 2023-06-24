namespace AzureDevOps.InnerSource.Configuration.Settings;

internal struct RepositoryAggregationSettings
{
    public const string SectionName = "RepositoryAggregation";

    public readonly struct RepositoryAggregationOverride
    {
        public string Description { get; init; }

        public string Installation { get; init; }
    }

    public Dictionary<string, RepositoryAggregationOverride>? Overrides { get; init; }
}