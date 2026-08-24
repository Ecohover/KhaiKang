using System.Net;
using System.Net.Http.Json;

namespace KhaiKang.Api.IntegrationTests;

public sealed class CommentMarkdownSecurityTests
{
    [Fact]
    public async Task CreateComment_WithSafeMarkdown_PreservesTheOriginalMarkdown()
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, "CMTSAFE");
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Markdown target");
        const string content = "# Result\n\n- Passed\n\n| Check | Result |\n| --- | --- |\n| Build | OK |\n\n`<safe-code>`\n\n[Evidence](https://example.com/evidence)";

        using var response = await PostCommentAsync(
            api,
            issue.Id,
            content,
            $"safe-markdown-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = await response.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(comment);
        Assert.Equal(content, comment.Content);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<iframe src=\"https://example.com\"></iframe>")]
    [InlineData("[payload](data:text/html;base64,abc)")]
    [InlineData("[local](file:///etc/passwd)")]
    [InlineData("![external](https://example.com/tracker.png)")]
    [InlineData("[relative](/unapproved/content)")]
    public async Task CreateComment_WithUnsafeMarkdown_IsTerminallyRejected(string content)
    {
        using var api = await AuthenticatedApiTestContext.CreateAsync();
        var project = await ApiTestData.CreateProjectAsync(api, $"SEC{Guid.NewGuid():N}"[..10]);
        var issue = await ApiTestData.CreateIssueAsync(api, project.Id, "Unsafe Markdown target");
        var idempotencyKey = $"unsafe-markdown-{Guid.NewGuid():N}";

        using var first = await PostCommentAsync(api, issue.Id, content, idempotencyKey);
        using var replay = await PostCommentAsync(api, issue.Id, content, idempotencyKey);

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal(first.StatusCode, replay.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
    }

    private static async Task<HttpResponseMessage> PostCommentAsync(
        AuthenticatedApiTestContext api,
        Guid issueId,
        string content,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/comments/issue/{issueId}")
        {
            Content = JsonContent.Create(new { content }),
        };
        request.Headers.Add(
            "X-XSRF-TOKEN",
            await AuthenticatedApiTestContext.GetCsrfTokenAsync(api.Client));
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await api.Client.SendAsync(request);
    }

    private sealed record CommentResponse(string Content);
}
