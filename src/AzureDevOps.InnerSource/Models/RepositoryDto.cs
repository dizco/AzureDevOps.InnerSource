namespace AzureDevOps.InnerSource.Models;

public record RepositoryDto
{
	public required string Project { get; init; }

	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required string? Description { get; init; }

	public required string? Installation { get; init; }

	public required StarsDto Stars { get; init; }

	public required IEnumerable<BadgeDto> Badges { get; init; }

	public required RepositoryMetadataDto Metadata { get; init; }
}

public record StarsDto(int Count);

public record BadgeDto(string Name, string Url);

public record RepositoryMetadataDto(string Url, DateTime? LastCommitDate);