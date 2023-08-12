namespace AzureDevOps.InnerSource.ADO.Models;

public record AdoRepositoryMetadata
{
	public required string Url { get; init; }

	public required DateTime? LastCommitDate { get; init; }
}