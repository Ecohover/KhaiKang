using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests;

public sealed class ProjectMemberOutcomeMappingTests
{
    [Fact]
    public async Task AddMember_MapsEveryOperationSpecificFailure()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "MEMADD");
        await api.Factory.AddActiveAccountAsync("member.candidate");
        await api.Factory.AddActiveAccountAsync("member.other");

        var missingProjectResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/members",
            new AddProjectMemberRequest(
                username: "member.candidate",
                roleCodes: ["contributor"]));
        Assert.Equal(HttpStatusCode.NotFound, missingProjectResponse.StatusCode);

        var missingAccountResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(
                username: "missing.account",
                roleCodes: ["contributor"]));
        Assert.Equal(HttpStatusCode.NotFound, missingAccountResponse.StatusCode);
        await AssertProblemCodeAsync(missingAccountResponse, "project_member_account_not_found");

        var invalidRoleResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(
                username: "member.candidate",
                roleCodes: ["unknown-role"]));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRoleResponse.StatusCode);

        var member = await AddMemberAsync(api, project.Id, "member.candidate");

        var duplicateResponse = await api.PostJsonAsync(
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(
                username: "member.candidate",
                roleCodes: ["contributor"]));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        await AssertProblemCodeAsync(duplicateResponse, "project_member_already_active");

        using var memberClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            memberClient,
            member.Username,
            AuthenticatedApiTestContext.TemporaryPassword);
        var forbiddenResponse = await AuthenticatedApiTestContext.SendJsonAsync(
            memberClient,
            HttpMethod.Post,
            $"/api/v1/projects/{project.Id}/members",
            new AddProjectMemberRequest(
                username: "member.other",
                roleCodes: ["contributor"]));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberRoles_MapsEveryOperationSpecificFailure()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "MEMUPD");
        await api.Factory.AddActiveAccountAsync("member.update");
        var member = await AddMemberAsync(api, project.Id, "member.update");
        var owner = await GetOwnerAsync(api, project.Id);

        var missingMemberResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{Guid.NewGuid()}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["reviewer"],
                version: 1));
        Assert.Equal(HttpStatusCode.NotFound, missingMemberResponse.StatusCode);

        var invalidRoleResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{member.Id}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["unknown-role"],
                version: member.Version));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRoleResponse.StatusCode);

        var lastOwnerResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{owner.Id}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["contributor"],
                version: owner.Version));
        Assert.Equal(HttpStatusCode.Conflict, lastOwnerResponse.StatusCode);
        await AssertProblemCodeAsync(lastOwnerResponse, "project_last_owner_required");

        using var memberClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            memberClient,
            member.Username,
            AuthenticatedApiTestContext.TemporaryPassword);
        var forbiddenResponse = await AuthenticatedApiTestContext.SendJsonAsync(
            memberClient,
            HttpMethod.Put,
            $"/api/v1/projects/{project.Id}/members/{owner.Id}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["owner"],
                version: owner.Version));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        var updateResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{member.Id}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["reviewer"],
                version: member.Version));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var staleResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{member.Id}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["contributor"],
                version: member.Version));
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        await AssertProblemCodeAsync(staleResponse, "project_member_version_conflict");
    }

    [Fact]
    public async Task RemoveMember_MapsEveryOperationSpecificFailure()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "MEMDEL");
        await api.Factory.AddActiveAccountAsync("member.remove");
        var member = await AddMemberAsync(api, project.Id, "member.remove");
        var owner = await GetOwnerAsync(api, project.Id);

        var missingMemberResponse = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/members/{Guid.NewGuid()}?version=1");
        Assert.Equal(HttpStatusCode.NotFound, missingMemberResponse.StatusCode);

        var lastOwnerResponse = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/members/{owner.Id}?version={owner.Version}");
        Assert.Equal(HttpStatusCode.Conflict, lastOwnerResponse.StatusCode);
        await AssertProblemCodeAsync(lastOwnerResponse, "project_last_owner_required");

        using var memberClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            memberClient,
            member.Username,
            AuthenticatedApiTestContext.TemporaryPassword);
        var forbiddenResponse = await DeleteAsync(
            memberClient,
            $"/api/v1/projects/{project.Id}/members/{owner.Id}?version={owner.Version}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        var updateResponse = await api.PutJsonAsync(
            $"/api/v1/projects/{project.Id}/members/{member.Id}/roles",
            new UpdateProjectMemberRolesRequest(
                roleCodes: ["reviewer"],
                version: member.Version));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedMember = Assert.IsType<ProjectMemberResponse>(
            await updateResponse.Content.ReadFromJsonAsync<ProjectMemberResponse>());

        var staleResponse = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/members/{member.Id}?version={member.Version}");
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        await AssertProblemCodeAsync(staleResponse, "project_member_version_conflict");

        var successResponse = await api.DeleteAsync(
            $"/api/v1/projects/{project.Id}/members/{member.Id}?version={updatedMember.Version}");
        Assert.Equal(HttpStatusCode.NoContent, successResponse.StatusCode);
    }

    private static async Task<ProjectMemberResponse> AddMemberAsync(
        AuthenticatedApiTestContext api,
        Guid projectId,
        string username)
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/projects/{projectId}/members",
            new AddProjectMemberRequest(
                username: username,
                roleCodes: ["contributor"]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ProjectMemberResponse>(
            await response.Content.ReadFromJsonAsync<ProjectMemberResponse>());
    }

    private static async Task<ProjectMemberResponse> GetOwnerAsync(
        AuthenticatedApiTestContext api,
        Guid projectId)
    {
        var members = await api.Client.GetFromJsonAsync<ProjectMemberResponse[]>(
            $"/api/v1/projects/{projectId}/members");
        Assert.NotNull(members);
        return Assert.Single(members, member => member.RoleCodes.Contains("owner"));
    }

    private static async Task<HttpResponseMessage> DeleteAsync(
        HttpClient client,
        string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
    }
}
