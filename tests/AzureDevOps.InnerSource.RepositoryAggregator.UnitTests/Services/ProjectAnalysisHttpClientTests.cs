using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzureDevOps.InnerSource.RepositoryAggregator.Services;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace AzureDevOps.InnerSource.RepositoryAggregator.UnitTests.Services;

public class ProjectAnalysisHttpClientTests
{
	[Fact]
	public async Task WithValidResponse_GetProjectLanguageAnalytics_ReturnsExpectedResponse()
	{
		// Arrange
		const string baseUrl = "https://localhost/myorg";
		var projectGuid = new Guid("43ad85db-de44-4f64-8e2e-2c6983b9d2e9");

		var handler = new MockHttpMessageHandler();
		handler.When(HttpMethod.Options, baseUrl + "/_apis/")
			.Respond("application/json",
				"{\"count\":1,\"value\": [{\"id\":\"5b02a779-1867-433f-90b7-d23ed5e33e57\",\"area\":\"projectanalysis\",\"resourceName\": \"languagemetrics\",\"routeTemplate\": \"{project}/_apis/{area}/{resource}\",\"resourceVersion\": 1,\"minVersion\": \"4.0\",\"maxVersion\": \"7.1\",\"releasedVersion\": \"7.0\"}]}");

		handler.When(HttpMethod.Get, $"https://localhost/myorg/{projectGuid}/_apis/projectanalysis/languagemetrics")
			.Respond("application/json",
				"{\"url\": \"https://dev.azure.com/myorg/43ad85db-de44-4f64-8e2e-2c6983b9d2e9/_apis/projectanalysis/languagemetrics\",\"resultPhase\": \"full\",\"languageBreakdown\": [{\"name\": \"JavaScript\",\"files\": 1,\"filesPercentage\": 100.0,\"bytes\": 124,\"languagePercentage\": 100.0}],\"repositoryLanguageAnalytics\": [{\"name\": \"gbourgault-2023-06-02-test\",\"resultPhase\": \"full\",\"updatedTime\": \"2023-06-22T14:11:22.34Z\",\"languageBreakdown\": [{\"name\": \"JavaScript\",\"files\": 1,\"filesPercentage\": 100.0,\"bytes\": 124,\"languagePercentage\": 100.0}],\"id\": \"f8cecd80-069c-4085-9a6d-caf7607bb515\"}],\"id\": \"43ad85db-de44-4f64-8e2e-2c6983b9d2e9\"}");

		var http = new ProjectAnalysisHttpClient(new Uri(baseUrl), handler, false);

		// Act
		var response = await http.GetProjectLanguageAnalyticsAsync(projectGuid, CancellationToken.None);

		// Assert
		response.Should().Be(HttpStatusCode.OK);
	}
}