namespace AzureDevOps.InnerSource.ADO.Models;

public record AdoProject
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}