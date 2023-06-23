using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.InnerSource.ADO.Configuration;

public class RepositoryAggregationOptions
{
	[Required]
	public string OutputFolder { get; set; } = "./";
}