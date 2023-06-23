namespace AzureDevOps.InnerSource.Services;

public sealed record Principal
{
	public required string Id { get; init; }

	public string? Email { get; init; }

	public bool Equals(Principal? other)
	{
		return string.Equals(Id, other?.Id, StringComparison.Ordinal);
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}
}