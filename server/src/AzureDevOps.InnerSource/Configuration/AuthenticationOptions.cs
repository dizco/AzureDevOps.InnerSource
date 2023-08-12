using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.InnerSource.Configuration;

public class AuthenticationOptions
{
	[Required]
	public string Key { get; set; } = null!;

	[Required]
	public string Issuer { get; set; } = null!;

	[Required]
	public string Audience { get; set; } = null!;
}