namespace AzureDevOps.InnerSource.Common;

public record Repository
{
	public required string Id { get; init; }
	public required string Project { get; init; }
	public required string Organization { get; init; }

	public override string ToString()
	{
		return Organization + "/" + Project + "/" + Id;
	}
}