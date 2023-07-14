using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Data.Tables;
using AzureDevOps.InnerSource.Common;
using AzureDevOps.InnerSource.Services;

namespace AzureDevOps.InnerSource.Storage;

public class StarEntity : ITableEntity
{
	public required string Repository { get; set; }
	public required string UserId { get; set; }
	public string? Email { get; set; }
	public required string PartitionKey { get; set; } = null!;
	public required string RowKey { get; set; } = null!;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}

public class StarCountEntity : ITableEntity
{
	public required int StarCount { get; set; }
	public required string Repository { get; set; }
	public required string PartitionKey { get; set; } = null!;
	public required string RowKey { get; set; } = null!;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}

public class StarTableRepository : IStarRepository
{
	private const string CountRowKey = "__COUNT__";
	private readonly TableClient _table;

	public StarTableRepository(TableClient table)
	{
		_table = table;
	}

	public async Task<int> GetStarCountAsync(Repository repository)
	{
		var entity = await _table.GetEntityIfExistsAsync<StarCountEntity>(HashRepository(repository), CountRowKey);
		return entity.HasValue ? entity.Value.StarCount : 0;
	}

	public async Task SetStarAsync(Repository repository, Principal principal)
	{
		var entity = await _table.GetEntityIfExistsAsync<StarEntity>(HashRepository(repository), principal.Id);
		if (entity.HasValue) return;

		await _table.UpsertEntityAsync(new StarEntity
		{
			PartitionKey = HashRepository(repository),
			RowKey = principal.Id,
			Repository = repository.ToString(),
			UserId = principal.Id,
			Email = principal.Email
		});

		// TODO: This is not safe for concurrent requests. 2 requests coming in at the same time might not increment the count with the expected value.
		var count = await GetStarCountAsync(repository);
		await SetStarCountAsync(repository, ++count);
	}

	public async Task SetStarCountAsync(Repository repository, int count)
	{
		await _table.UpsertEntityAsync(new StarCountEntity
		{
			PartitionKey = HashRepository(repository),
			RowKey = CountRowKey,
			StarCount = count,
			Repository = repository.ToString()
		});
	}

	private static string HashRepository(Repository repository)
	{
		var repositoryId = repository.ToString().ToLowerInvariant();

		StringBuilder sb = new();
		foreach (var b in HashString(repositoryId))
			sb.Append(b.ToString("X2"));

		return sb.ToString();
	}

	private static byte[] HashString(string input)
	{
		var bytes = Encoding.UTF8.GetBytes(input);
		return SHA256.HashData(bytes);
	}
}