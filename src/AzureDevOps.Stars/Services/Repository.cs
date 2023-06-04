namespace AzureDevOps.Stars.Services;

public record Repository
{
	public required string Name { get; init; }
	public required string Project { get; init; }
	public required string Organization { get; init; }

	public override string ToString()
	{
		return Organization + "/" + Project + "/" + Name;
	}
}