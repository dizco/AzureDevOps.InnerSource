namespace AzureDevOps.InnerSource.Common.Services;

public interface IBadgeTokenService
{
	string GenerateBadgeJwt(string projectName, string repositoryId, DateTime notBefore, DateTime expires);
}