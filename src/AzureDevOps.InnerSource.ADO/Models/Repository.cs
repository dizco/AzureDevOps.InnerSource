namespace AzureDevOps.InnerSource.ADO.Models;

public record Repository
{
	public required string Project { get; init; }

	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required string? Description { get; init; }

	public required string? Installation { get; init; }

	public required string WebUrl { get; init; }

	public required ProgrammingLanguage? Language { get; init; }

	public required List<Badge> Badges { get; init; }
}