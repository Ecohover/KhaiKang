using System.Net;
using System.Net.Http.Json;
using KhaiKang.CommonUtils.Models;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class ProjectEndpointsTests(ApiIntegrationTestFactory factory)
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly ApiIntegrationTestFactory _factory = factory;

    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    [Fact]
    public async Task ProjectFlow_CreatesOwnerMembershipAndUpdatesBasicInformation()
    {
        var csrfToken = await GetCsrfTokenAsync();
        var initializeResponse = await PostAsync(
            "/api/v1/setup/initialize",
            content: null,
            csrfToken);
        initializeResponse.EnsureSuccessStatusCode();
        var credentials = await initializeResponse.Content.ReadFromJsonAsync<InitializeAdminResponse>();
        Assert.NotNull(credentials);

        var loginResponse = await PostAsync(
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest("admin", credentials.InitialPassword, false)),
            csrfToken);
        loginResponse.EnsureSuccessStatusCode();

        var createResponse = await PostAsync(
            "/api/v1/projects",
            JsonContent.Create(new CreateProjectRequest("core", "Core Project", "First project")),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(created);
        Assert.Equal("CORE", created.Code);
        Assert.Contains("Owner", created.CurrentUserRoles);
        Assert.Contains("project.read", created.CurrentUserPermissions);
        Assert.Contains("project.update", created.CurrentUserPermissions);

        await _factory.AddActiveAccountAsync("reviewer");
        var roles = await _client.GetFromJsonAsync<ProjectRoleResponse[]>(
            $"/api/v1/projects/{created.Id}/roles");
        Assert.NotNull(roles);
        Assert.Equal(4, roles.Length);

        var initialMembers = await _client.GetFromJsonAsync<ProjectMemberResponse[]>(
            $"/api/v1/projects/{created.Id}/members");
        Assert.NotNull(initialMembers);
        var owner = Assert.Single(initialMembers);
        Assert.Equal("admin", owner.Username);
        Assert.Contains("owner", owner.RoleCodes);

        var addMemberResponse = await PostAsync(
            $"/api/v1/projects/{created.Id}/members",
            JsonContent.Create(new AddProjectMemberRequest(
                username: "reviewer",
                roleCodes: ["contributor"])),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, addMemberResponse.StatusCode);
        var addedMember = await addMemberResponse.Content.ReadFromJsonAsync<ProjectMemberResponse>();
        Assert.NotNull(addedMember);
        Assert.Equal("reviewer", addedMember.Username);
        Assert.Equal(["contributor"], addedMember.RoleCodes);

        var issueMetadata = await _client.GetFromJsonAsync<IssueMetadataResponse>(
            $"/api/v1/projects/{created.Id}/issues/metadata");
        Assert.NotNull(issueMetadata);
        Assert.Contains(issueMetadata.Types, item => item.Code == "task");
        Assert.Contains(issueMetadata.Statuses, item => item.Code == "created");
        Assert.Contains(issueMetadata.Priorities, item => item.Code == "high");

        var createIssueResponse = await PostAsync(
            $"/api/v1/projects/{created.Id}/issues",
            JsonContent.Create(new
            {
                title = "Build the first task",
                typeCode = "task",
            }),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createIssueResponse.StatusCode);
        var createdIssue = await createIssueResponse.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(createdIssue);
        Assert.Equal("CORE-1", createdIssue.Key);
        Assert.Equal("created", createdIssue.StatusCode);
        Assert.Equal("medium", createdIssue.PriorityCode);
        Assert.Null(createdIssue.AssigneeAccountId);

        var invalidAssigneeResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/assignee",
            JsonContent.Create(new UpdateIssueAssigneeRequest(Guid.NewGuid(), createdIssue.Version)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.BadRequest, invalidAssigneeResponse.StatusCode);

        var assignIssueResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/assignee",
            JsonContent.Create(new UpdateIssueAssigneeRequest(
                addedMember.AccountId,
                createdIssue.Version)),
            await GetCsrfTokenAsync());
        assignIssueResponse.EnsureSuccessStatusCode();
        var assignedIssue = await assignIssueResponse.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(assignedIssue);
        Assert.Equal("reviewer", assignedIssue.AssigneeUsername);

        var issues = await _client.GetFromJsonAsync<PagedResult<IssueResponse>>(
            $"/api/v1/projects/{created.Id}/issues?page=1&pageSize=1");
        Assert.NotNull(issues);
        Assert.Equal(1, issues.Page);
        Assert.Equal(1, issues.PageSize);
        Assert.Equal(1, issues.TotalCount);
        Assert.Equal(1, issues.TotalPages);
        Assert.False(issues.HasPreviousPage);
        Assert.False(issues.HasNextPage);
        Assert.Equal(assignedIssue.Id, Assert.Single(issues.Items).Id);

        var invalidPagingResponse = await _client.GetAsync(
            $"/api/v1/projects/{created.Id}/issues?page=0&pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPagingResponse.StatusCode);

        var updateIssueStatusResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/status",
            JsonContent.Create(new UpdateIssueStatusRequest("in_progress", assignedIssue.Version)),
            await GetCsrfTokenAsync());
        updateIssueStatusResponse.EnsureSuccessStatusCode();
        var updatedIssue = await updateIssueStatusResponse.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(updatedIssue);
        Assert.Equal("in_progress", updatedIssue.StatusCode);
        Assert.Equal(assignedIssue.Version + 1, updatedIssue.Version);

        var staleIssueStatusResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/status",
            JsonContent.Create(new UpdateIssueStatusRequest("completed", assignedIssue.Version)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, staleIssueStatusResponse.StatusCode);

        var updateIssueResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}",
            JsonContent.Create(new UpdateIssueRequest(
                title: "Build and verify the first task",
                typeCode: "story",
                priorityCode: "critical",
                version: updatedIssue.Version)
            {
                Description = "## Updated task description\n\n- One\n- Two",
                UserStory = "As a **project member**, I want a complete task form.",
                DefinitionOfDone = "- [ ] The dedicated editor is available.",
                CompletionSummary = "See [evidence](https://example.test/evidence).",
            }),
            await GetCsrfTokenAsync());
        updateIssueResponse.EnsureSuccessStatusCode();
        var editedIssue = await updateIssueResponse.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(editedIssue);
        Assert.Equal("Build and verify the first task", editedIssue.Title);
        Assert.Equal("story", editedIssue.TypeCode);
        Assert.Equal("critical", editedIssue.PriorityCode);
        Assert.Equal("See [evidence](https://example.test/evidence).", editedIssue.CompletionSummary);
        Assert.Equal("## Updated task description\n\n- One\n- Two", editedIssue.Description);
        Assert.Equal("As a **project member**, I want a complete task form.", editedIssue.UserStory);
        Assert.Equal("- [ ] The dedicated editor is available.", editedIssue.DefinitionOfDone);

        var fetchedIssue = await _client.GetFromJsonAsync<IssueResponse>(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}");
        Assert.NotNull(fetchedIssue);
        Assert.Equal(editedIssue.Version, fetchedIssue.Version);

        var attachmentBytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        using var attachmentContent = new MultipartFormDataContent();
        var attachmentFile = new ByteArrayContent(attachmentBytes);
        attachmentFile.Headers.ContentType = new("image/png");
        attachmentContent.Add(attachmentFile, "file", "evidence.png");
        var uploadAttachmentResponse = await PostAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/attachments",
            attachmentContent,
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, uploadAttachmentResponse.StatusCode);
        var attachment = await uploadAttachmentResponse.Content.ReadFromJsonAsync<IssueAttachmentResponse>();
        Assert.NotNull(attachment);
        Assert.Equal("evidence.png", attachment.OriginalFileName);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(attachmentBytes.Length, attachment.FileSize);
        Assert.Equal(64, attachment.FileHash.Length);

        var listedAttachments = await _client.GetFromJsonAsync<IssueAttachmentResponse[]>(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/attachments");
        Assert.NotNull(listedAttachments);
        Assert.Equal(attachment.Id, Assert.Single(listedAttachments).Id);

        var attachmentDownload = await _client.GetAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/attachments/{attachment.Id}/content?inline=true");
        attachmentDownload.EnsureSuccessStatusCode();
        Assert.Equal("image/png", attachmentDownload.Content.Headers.ContentType?.MediaType);
        Assert.Equal(attachmentBytes, await attachmentDownload.Content.ReadAsByteArrayAsync());

        var deleteAttachmentResponse = await DeleteAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/attachments/{attachment.Id}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.NoContent, deleteAttachmentResponse.StatusCode);
        var attachmentsAfterDelete = await _client.GetFromJsonAsync<IssueAttachmentResponse[]>(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/attachments");
        Assert.NotNull(attachmentsAfterDelete);
        Assert.Empty(attachmentsAfterDelete);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/attachments/{attachment.Id}/content")).StatusCode);

        var updateRolesResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/members/{addedMember.Id}/roles",
            JsonContent.Create(new UpdateProjectMemberRolesRequest(
                roleCodes: ["reviewer"],
                version: addedMember.Version)),
            await GetCsrfTokenAsync());
        updateRolesResponse.EnsureSuccessStatusCode();
        var updatedMember = await updateRolesResponse.Content
            .ReadFromJsonAsync<ProjectMemberResponse>();
        Assert.NotNull(updatedMember);
        Assert.Equal(["reviewer"], updatedMember.RoleCodes);

        using var reviewerClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
        var reviewerCsrfToken = await GetCsrfTokenAsync(reviewerClient);
        var reviewerLoginResponse = await PostAsync(
            reviewerClient,
            "/api/v1/auth/login",
            JsonContent.Create(new LoginRequest(
                "reviewer",
                "Temporary-Pass-123!",
                false)),
            reviewerCsrfToken);
        reviewerLoginResponse.EnsureSuccessStatusCode();

        var reviewerUnassignResponse = await PutAsync(
            reviewerClient,
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/assignee",
            JsonContent.Create(new UpdateIssueAssigneeRequest(null, editedIssue.Version)),
            await GetCsrfTokenAsync(reviewerClient));
        reviewerUnassignResponse.EnsureSuccessStatusCode();
        var unassignedIssue = await reviewerUnassignResponse.Content
            .ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(unassignedIssue);
        Assert.Null(unassignedIssue.AssigneeAccountId);

        var filteredIssues = await _client.GetFromJsonAsync<PagedResult<IssueResponse>>(
            $"/api/v1/projects/{created.Id}/issues?page=1&pageSize=20&search=CORE-1&typeCode=story&statusCode=in_progress&priorityCode=critical&unassigned=true&sortBy=updatedAt&sortDirection=desc");
        Assert.NotNull(filteredIssues);
        Assert.Equal(1, filteredIssues.TotalCount);
        Assert.Equal(unassignedIssue.Id, Assert.Single(filteredIssues.Items).Id);

        var contradictoryFilterResponse = await _client.GetAsync(
            $"/api/v1/projects/{created.Id}/issues?page=1&pageSize=20&assigneeAccountId={addedMember.AccountId}&unassigned=true");
        Assert.Equal(HttpStatusCode.BadRequest, contradictoryFilterResponse.StatusCode);

        var reviewerEditResponse = await PutAsync(
            reviewerClient,
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}",
            JsonContent.Create(new UpdateIssueRequest(
                title: "Reviewer must not edit content",
                typeCode: "story",
                priorityCode: "critical",
                version: unassignedIssue.Version)),
            await GetCsrfTokenAsync(reviewerClient));
        Assert.Equal(HttpStatusCode.Forbidden, reviewerEditResponse.StatusCode);

        var removeLastOwnerResponse = await DeleteAsync(
            $"/api/v1/projects/{created.Id}/members/{owner.Id}?version={owner.Version}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, removeLastOwnerResponse.StatusCode);

        var removeMemberResponse = await DeleteAsync(
            $"/api/v1/projects/{created.Id}/members/{updatedMember.Id}?version={updatedMember.Version}",
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.NoContent, removeMemberResponse.StatusCode);

        var finalMembers = await _client.GetFromJsonAsync<ProjectMemberResponse[]>(
            $"/api/v1/projects/{created.Id}/members");
        Assert.NotNull(finalMembers);
        Assert.Single(finalMembers);

        var projects = await _client.GetFromJsonAsync<ProjectResponse[]>("/api/v1/projects");
        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal(created.Id, projects[0].Id);

        var updateResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}",
            JsonContent.Create(new UpdateProjectRequest(
                "Core Project Updated",
                "Updated description",
                "inactive",
                created.Version)),
            await GetCsrfTokenAsync());
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Core Project Updated", updated.Name);
        Assert.Equal("inactive", updated.Status);
        Assert.Equal(created.Version + 1, updated.Version);

        var inactiveProjectsIssues = await _client.GetFromJsonAsync<PagedResult<IssueResponse>>(
            $"/api/v1/projects/{created.Id}/issues");
        Assert.NotNull(inactiveProjectsIssues);
        Assert.Single(inactiveProjectsIssues.Items);

        var inactiveStatusResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}/issues/{createdIssue.Id}/status",
            JsonContent.Create(new UpdateIssueStatusRequest(
                "completed",
                unassignedIssue.Version)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, inactiveStatusResponse.StatusCode);
        var inactiveProblem = await inactiveStatusResponse.Content
            .ReadFromJsonAsync<ApiProblem>();
        Assert.NotNull(inactiveProblem);
        Assert.Equal("project_inactive", inactiveProblem.Code);

        var staleUpdateResponse = await PutAsync(
            $"/api/v1/projects/{created.Id}",
            JsonContent.Create(new UpdateProjectRequest(
                "Stale update",
                null,
                "active",
                created.Version)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);
    }

    private async Task<string> GetCsrfTokenAsync()
    {
        return await GetCsrfTokenAsync(_client);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/v1/auth/csrf-token");
        Assert.NotNull(response);
        return response.Token;
    }

    private async Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAsync(
        string path,
        HttpContent content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        HttpContent? content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PutAsync(
        HttpClient client,
        string path,
        HttpContent content,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = content };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAsync(string path, string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return await _client.SendAsync(request);
    }

    private sealed record ApiProblem(string? Code);
}
