using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.CommentManagement.Contracts;

namespace KhaiKang.Api.IntegrationTests.Identity;

public sealed partial class Pf01AiCommentScopeTests
{
    [Fact]
    public async Task CommentAccess_WithHumanSession_PreservesReadCreateAndCsrfBoundaries()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "PF01HUM");
        var task = await CreateIssueAsync(api, project.Id, "Human Comment target", "task");
        var path = IssueCommentPath(task.Id);

        using var humanList = await api.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, humanList.StatusCode);
        var humanPage = await humanList.Content.ReadFromJsonAsync<CommentPageResponse>();
        Assert.NotNull(humanPage);
        Assert.True(humanPage.CanCreate);
        using var humanCreate = await PostHumanCommentAsync(
            api.Client,
            path,
            "Human session Comment.",
            $"human-valid-{Guid.NewGuid():N}",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        Assert.Equal(HttpStatusCode.Created, humanCreate.StatusCode);
        using var missingCsrf = await PostHumanCommentAsync(
            api.Client,
            path,
            "Missing CSRF must fail.",
            $"human-no-csrf-{Guid.NewGuid():N}",
            csrfToken: null);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var devops = await CreateAiBearerAsync(api, "ai-pf01-human-boundary", "devops_ai");
        using var bearer = BearerClient(api, devops.Token);
        using var bearerCreate = await PostCommentAsync(
            bearer,
            path,
            "Bearer does not use CSRF.",
            $"bearer-no-csrf-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Created, bearerCreate.StatusCode);

        var outsider = await CreateHumanAccountAsync(api, "pf01-human-outsider");
        using var outsiderClient = api.CreateClient();
        await AuthenticatedApiTestContext.LoginAsync(
            outsiderClient,
            outsider.Username,
            outsider.InitialPassword);
        using var outsiderList = await outsiderClient.GetAsync(path);
        using var outsiderCreate = await PostHumanCommentAsync(
            outsiderClient,
            path,
            "Outsider Comment.",
            $"human-outsider-{Guid.NewGuid():N}",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(outsiderClient));
        Assert.Equal(HttpStatusCode.NotFound, outsiderList.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outsiderCreate.StatusCode);
    }
}
