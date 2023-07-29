namespace AzureDevOps.InnerSource.ADO.Models;

public record AdoRepository
{
	public required string Organization { get; init; }

	public required string Project { get; init; }

	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required string? Description { get; init; }

	public required string? Installation { get; init; }

	public required ProgrammingLanguage? Language { get; init; }

	public required List<Badge> Badges { get; init; }

	public required AdoRepositoryMetadata Metadata { get; init; }
}