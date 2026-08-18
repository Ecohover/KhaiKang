using System.Net;
using System.Net.Http.Json;
using KhaiKang.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Api.IntegrationTests;

public sealed class SystemEndpointsTests(ApiIntegrationTestFactory factory)
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetSystemInfo_ReturnsResourceWithoutEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/system/info");

        response.EnsureSuccessStatusCode();
        var systemInfo = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();

        Assert.NotNull(systemInfo);
        Assert.Equal("KhaiKang.Api", systemInfo.ServiceName);
        Assert.Equal("0.1.0", systemInfo.Version);
    }

    [Fact]
    public async Task GetOpenApiContract_ReturnsCanonicalDocument()
    {
        var response = await _client.GetAsync("/openapi/v1.yaml");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/yaml", response.Content.Headers.ContentType?.MediaType);
        var contract = await response.Content.ReadAsStringAsync();
        Assert.Contains("operationId: Login", contract, StringComparison.Ordinal);
        Assert.Contains("AuthenticatedUserResponse:", contract, StringComparison.Ordinal);
        Assert.Contains("operationId: UploadIssueAttachment", contract, StringComparison.Ordinal);
        Assert.Contains("IssueAttachmentResponse:", contract, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnknownResource_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/unknown-resource");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.False(problemDetails.Extensions.ContainsKey("traceId"));
    }
}
