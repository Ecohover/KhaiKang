using System.Net;
using System.Net.Http.Json;
using KhaiKang.Contracts.System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class SystemEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
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
