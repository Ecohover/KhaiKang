using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TestManagementEndpointsTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    [Fact]
    public async Task TestManagement_CreateWorkspaceSuiteAndCase_PersistsOrderedSteps()
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

        var explicitResponse = await CreateWorkspaceAsync("Regression", "qa");
        Assert.Equal(HttpStatusCode.Created, explicitResponse.StatusCode);
        var explicitWorkspace =
            await explicitResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(explicitWorkspace);
        Assert.Equal("QA", explicitWorkspace.Prefix);

        var generatedResponse = await CreateWorkspaceAsync("Web Checkout", null);
        Assert.Equal(HttpStatusCode.Created, generatedResponse.StatusCode);
        var generatedWorkspace =
            await generatedResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(generatedWorkspace);
        Assert.Equal("WEBCHE", generatedWorkspace.Prefix);

        var generatedConflictResponse = await CreateWorkspaceAsync("Web Checkers", null);
        Assert.Equal(HttpStatusCode.Created, generatedConflictResponse.StatusCode);
        var generatedConflictWorkspace =
            await generatedConflictResponse.Content.ReadFromJsonAsync<TestWorkspaceResponse>();
        Assert.NotNull(generatedConflictWorkspace);
        Assert.Equal("WEBCHE2", generatedConflictWorkspace.Prefix);

        var duplicateResponse = await CreateWorkspaceAsync("API Regression", "QA");
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var invalidResponse = await CreateWorkspaceAsync("Invalid Prefix", "1bad");
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        var suiteResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/suites",
            JsonContent.Create(new CreateTestSuiteRequest(
                null,
                "Authentication",
                "Authentication scenarios",
                1)),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, suiteResponse.StatusCode);
        var suite = await suiteResponse.Content.ReadFromJsonAsync<TestSuiteResponse>();
        Assert.NotNull(suite);

        var tagResponse = await PostAsync(
            "/api/v1/test-tags",
            JsonContent.Create(new CreateTestTagRequest("smoke", "Critical sign-in coverage.")),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, tagResponse.StatusCode);
        var tag = await tagResponse.Content.ReadFromJsonAsync<TestTagResponse>();
        Assert.NotNull(tag);

        var createCaseResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases",
            JsonContent.Create(new CreateTestCaseRequest(
                suite.Id,
                "Sign in with valid credentials",
                "Verifies the normal sign-in flow.",
                "An active account exists.",
                "The user reaches the home page.",
                1,
                [
                    new("Open the sign-in page.", "The sign-in form is displayed."),
                    new("Submit valid credentials.", "The home page is displayed."),
                ],
                TagIds: [tag.Id])),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createCaseResponse.StatusCode);
        var testCase = await createCaseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(testCase);
        Assert.Equal(suite.Id, testCase.SuiteId);
        Assert.Equal(1, testCase.CaseNo);
        Assert.Equal(tag.Id, Assert.Single(testCase.Tags).Id);
        Assert.Collection(
            testCase.Steps,
            step => Assert.Equal(1, step.StepNo),
            step => Assert.Equal(2, step.StepNo));

        var caseListResponse = await _client.GetAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases?suiteId={suite.Id}");
        caseListResponse.EnsureSuccessStatusCode();
        var cases = await caseListResponse.Content.ReadFromJsonAsync<TestCaseResponse[]>();
        var listedCase = Assert.Single(Assert.IsType<TestCaseResponse[]>(cases));
        Assert.Equal(testCase.Id, listedCase.Id);
        Assert.Equal(2, listedCase.Steps.Count);

        var getCaseResponse = await _client.GetAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{testCase.Id}");
        getCaseResponse.EnsureSuccessStatusCode();
        var fetchedCase = await getCaseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(fetchedCase);
        Assert.Equal(testCase.Id, fetchedCase.Id);

        var updateCaseRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{testCase.Id}")
        {
            Content = JsonContent.Create(new UpdateTestCaseRequest(
                suite.Id,
                "Sign in with valid credentials - Updated",
                "## Updated description\n\n- visible to the team",
                "**Updated** preconditions.",
                "[Updated expected result](https://example.test/expected).",
                2,
                "active",
                testCase.Version,
                [
                    new("1. Enter **valid** credentials.", "The [home page](https://example.test/home) is displayed."),
                ],
                TagIds: [tag.Id])),
        };
        updateCaseRequest.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync());
        var updateCaseResponse = await _client.SendAsync(updateCaseRequest);
        Assert.Equal(HttpStatusCode.OK, updateCaseResponse.StatusCode);
        var updatedCase = await updateCaseResponse.Content.ReadFromJsonAsync<TestCaseResponse>();
        Assert.NotNull(updatedCase);
        Assert.Equal("Sign in with valid credentials - Updated", updatedCase.Title);
        Assert.Equal(2, updatedCase.Version);
        Assert.Equal("## Updated description\n\n- visible to the team", updatedCase.Description);
        Assert.Equal("**Updated** preconditions.", updatedCase.Preconditions);
        Assert.Equal("[Updated expected result](https://example.test/expected).", updatedCase.OverallExpectedResult);
        Assert.Equal(tag.Id, Assert.Single(updatedCase.Tags).Id);
        Assert.Single(updatedCase.Steps);

        var attachmentBytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        using var attachmentContent = new MultipartFormDataContent();
        var attachmentFile = new ByteArrayContent(attachmentBytes);
        attachmentFile.Headers.ContentType = new("image/png");
        attachmentContent.Add(attachmentFile, "file", "case-evidence.png");
        var uploadAttachmentResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{updatedCase.Id}/attachments",
            attachmentContent,
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, uploadAttachmentResponse.StatusCode);
        var attachment = await uploadAttachmentResponse.Content.ReadFromJsonAsync<TestCaseAttachmentResponse>();
        Assert.NotNull(attachment);
        Assert.Equal(updatedCase.Id, attachment.TestCaseId);
        Assert.Equal("case-evidence.png", attachment.OriginalFileName);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(attachmentBytes.Length, attachment.FileSize);
        Assert.Equal(64, attachment.FileHash.Length);

        var listedAttachments = await _client.GetFromJsonAsync<TestCaseAttachmentResponse[]>(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{updatedCase.Id}/attachments");
        Assert.NotNull(listedAttachments);
        Assert.Equal(attachment.Id, Assert.Single(listedAttachments).Id);

        var attachmentDownload = await _client.GetAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{updatedCase.Id}/attachments/{attachment.Id}/content?inline=true");
        attachmentDownload.EnsureSuccessStatusCode();
        Assert.Equal("image/png", attachmentDownload.Content.Headers.ContentType?.MediaType);
        Assert.Equal(attachmentBytes, await attachmentDownload.Content.ReadAsByteArrayAsync());

        var createPlanResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/plans",
            JsonContent.Create(new
            {
                description = "Fixed release scope.",
                caseIds = new[] { updatedCase.Id },
            }),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createPlanResponse.StatusCode);
        var draftPlan = await createPlanResponse.Content.ReadFromJsonAsync<TestPlanResponse>();
        Assert.NotNull(draftPlan);
        Assert.Equal("draft", draftPlan.Status);
        Assert.Matches("^TestPlan\\d{8}$", draftPlan.Name);
        Assert.Equal(1, draftPlan.PlanNo);
        Assert.Equal("QA-TP1", draftPlan.Code);
        Assert.Equal(updatedCase.Id, Assert.Single(draftPlan.Items).CaseId);

        var activatePlanResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/plans/{draftPlan.Id}",
            new UpdateTestPlanRequest(
                draftPlan.Name,
                draftPlan.Description,
                "active",
                draftPlan.Version,
                [updatedCase.Id]));
        activatePlanResponse.EnsureSuccessStatusCode();
        var activePlan = await activatePlanResponse.Content.ReadFromJsonAsync<TestPlanResponse>();
        Assert.NotNull(activePlan);
        Assert.Equal("active", activePlan.Status);

        var createRunResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs",
            JsonContent.Create(new CreateTestRunRequest(activePlan.Id, "Release 2026.07")),
            await GetCsrfTokenAsync());
        Assert.Equal(HttpStatusCode.Created, createRunResponse.StatusCode);
        var run = await createRunResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(run);
        Assert.Equal(1, run.RunNo);
        Assert.Equal("QA-TP1-R1", run.Code);
        var runItem = Assert.Single(run.Items);
        var runStep = Assert.Single(runItem.Steps);
        Assert.Equal("Sign in with valid credentials - Updated", runItem.CaseTitle);
        Assert.Equal("1. Enter **valid** credentials.", runStep.Action);

        var changeSourceResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/cases/{updatedCase.Id}",
            new UpdateTestCaseRequest(
                suite.Id,
                "Source changed after run",
                "Changed source description.",
                null,
                null,
                2,
                "active",
                updatedCase.Version,
                [new("Changed source step.", "Changed source expected result.")]));
        changeSourceResponse.EnsureSuccessStatusCode();

        var fetchedRunResponse = await _client.GetAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{run.Id}");
        fetchedRunResponse.EnsureSuccessStatusCode();
        var fetchedRun = await fetchedRunResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(fetchedRun);
        runItem = Assert.Single(fetchedRun.Items);
        runStep = Assert.Single(runItem.Steps);
        Assert.Equal("Sign in with valid credentials - Updated", runItem.CaseTitle);
        Assert.Equal("1. Enter **valid** credentials.", runStep.Action);

        var stepResultResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{run.Id}/items/{runItem.Id}/steps/{runStep.Id}",
            new RecordTestResultRequest("passed", "Displayed as expected.", runStep.Version));
        stepResultResponse.EnsureSuccessStatusCode();
        var stepRecordedRun =
            await stepResultResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(stepRecordedRun);
        Assert.Equal("in_progress", stepRecordedRun.Status);

        runItem = Assert.Single(stepRecordedRun.Items);
        var itemResultResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{run.Id}/items/{runItem.Id}",
            new RecordTestResultRequest("passed", "Scenario passed.", runItem.Version));
        itemResultResponse.EnsureSuccessStatusCode();
        var itemRecordedRun =
            await itemResultResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(itemRecordedRun);
        Assert.Equal(1, itemRecordedRun.Progress.Passed);

        var completeResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{run.Id}/status",
            new UpdateTestRunStatusRequest(
                "completed", "Release accepted.", itemRecordedRun.Version));
        completeResponse.EnsureSuccessStatusCode();
        var completedRun = await completeResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(completedRun);
        Assert.Equal("completed", completedRun.Status);

        runItem = Assert.Single(completedRun.Items);
        var immutableResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{run.Id}/items/{runItem.Id}",
            new RecordTestResultRequest("failed", "Must not change.", runItem.Version));
        Assert.Equal(HttpStatusCode.Conflict, immutableResponse.StatusCode);

        var retryRunResponse = await PostAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs",
            JsonContent.Create(new CreateTestRunRequest(run.PlanId, "Cancelled run can resume.")),
            await GetCsrfTokenAsync());
        retryRunResponse.EnsureSuccessStatusCode();
        var retryRun = await retryRunResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(retryRun);

        var cancelledResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{retryRun.Id}/status",
            new UpdateTestRunStatusRequest("cancelled", "Paused for later.", retryRun.Version));
        cancelledResponse.EnsureSuccessStatusCode();
        var cancelledRun = await cancelledResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(cancelledRun);
        Assert.Equal("cancelled", cancelledRun.Status);
        Assert.NotNull(cancelledRun.CompletedAt);

        var restartResponse = await PutAsync(
            $"/api/v1/test-workspaces/{explicitWorkspace.Id}/runs/{cancelledRun.Id}/status",
            new UpdateTestRunStatusRequest("in_progress", null, cancelledRun.Version));
        restartResponse.EnsureSuccessStatusCode();
        var restartedRun = await restartResponse.Content.ReadFromJsonAsync<TestRunResponse>();
        Assert.NotNull(restartedRun);
        Assert.Equal("in_progress", restartedRun.Status);
        Assert.NotNull(restartedRun.StartedAt);
        Assert.Null(restartedRun.CompletedAt);
    }

    private async Task<HttpResponseMessage> CreateWorkspaceAsync(string name, string? prefix)
    {
        return await PostAsync(
            "/api/v1/test-workspaces",
            JsonContent.Create(new CreateTestWorkspaceRequest(name, prefix, null)),
            await GetCsrfTokenAsync());
    }

    private async Task<string> GetCsrfTokenAsync()
    {
        var response = await _client.GetAsync("/api/v1/auth/csrf-token");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        Assert.NotNull(payload);
        return payload.Token;
    }

    private Task<HttpResponseMessage> PostAsync(
        string path,
        HttpContent? content,
        string csrfToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content,
        };
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        return _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAsync<T>(string path, T value)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(value),
        };
        request.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync());
        return await _client.SendAsync(request);
    }
}
