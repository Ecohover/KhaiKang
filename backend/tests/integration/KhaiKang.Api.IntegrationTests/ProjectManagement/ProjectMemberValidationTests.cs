using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Api.IntegrationTests;

public sealed class ProjectMemberValidationTests
{
    [Fact]
    public async Task AddProjectMember_WhenUsernameIsNull_ReturnsUsernameValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);

        var response = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new
            {
                username = (string?)null,
                roleCodes = new[] { "contributor" },
            });

        await AssertValidationProblemAsync(response, "username");
    }

    [Fact]
    public async Task AddProjectMember_WhenRoleCodesIsNull_ReturnsRoleCodesValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);

        var response = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new
            {
                username = "reviewer",
                roleCodes = (string[]?)null,
            });

        await AssertValidationProblemAsync(response, "roleCodes");
    }

    [Fact]
    public async Task UpdateProjectMemberRoles_WhenRoleCodesIsNull_ReturnsRoleCodesValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);

        var response = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{Guid.NewGuid()}/roles",
            new
            {
                roleCodes = (string[]?)null,
                version = 1,
            });

        await AssertValidationProblemAsync(response, "roleCodes");
    }

    [Fact]
    public async Task UpdateProjectMemberRoles_WhenVersionIsNotPositive_ReturnsVersionValidationProblem()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api);

        var response = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{Guid.NewGuid()}/roles",
            new
            {
                roleCodes = new[] { "contributor" },
                version = 0,
            });

        await AssertValidationProblemAsync(response, "version");
    }

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedError)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(expectedError, problem.Errors);
    }
}
