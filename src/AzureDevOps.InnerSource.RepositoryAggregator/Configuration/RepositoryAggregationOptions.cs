using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.InnerSource.RepositoryAggregator.Configuration;

public class RepositoryAggregationOptions
{
	[Required]
	public string OutputFolder { get; set; } = "./";
}