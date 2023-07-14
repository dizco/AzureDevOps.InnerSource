namespace AzureDevOps.InnerSource.ADO.Models;

public record Project
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}