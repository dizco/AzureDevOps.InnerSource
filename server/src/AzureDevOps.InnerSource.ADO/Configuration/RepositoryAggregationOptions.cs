using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.InnerSource.ADO.Configuration;

public class RepositoryAggregationOptions
{
    [Required]
    public string OutputFolder { get; set; } = "./";

    [Required, Url]
    public string BadgeServerUrl { get; set; } = null!;

    [Required]
    public Dictionary<string, RepositoryAggregationOverride> Overrides { get; set; } = new();

    public readonly struct RepositoryAggregationOverride
    {
        public string Description { get; init; }

        public string Installation { get; init; }
    }
}