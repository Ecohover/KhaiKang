using System.Net;
using System.Net.Http.Json;
using KhaiKang.CommonUtils.Storage;
using KhaiKang.Modules.ProjectManagement.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KhaiKang.Api.IntegrationTests;

public sealed class AttachmentStorageBoundaryTests
{
    [Fact]
    public async Task OpenAttachment_WhenStorageContentIsUnavailable_ReturnsServiceUnavailable()
    {
        var storage = new ControllableFileStorage();
        using var api = await AuthenticatedApiTestContext.CreateAsync(services =>
        {
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage>(storage);
        });
        var project = await ApiTestData.CreateProjectAsync(api);
        var issue = await ApiTestData.CreateIssueAsync(
            api,
            project.Id,
            "Attachment boundary");

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("attachment evidence"u8.ToArray());
        fileContent.Headers.ContentType = new("text/plain");
        uploadContent.Add(fileContent, "file", "evidence.txt");
        var uploadResponse = await api.PostAsync(
            $"/api/v1/projects/{project.Id}/issues/{issue.Id}/attachments",
            uploadContent);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var attachment = Assert.IsType<IssueAttachmentResponse>(
            await uploadResponse.Content.ReadFromJsonAsync<IssueAttachmentResponse>());

        storage.ReadsAreAvailable = false;

        var contentResponse = await api.Client.GetAsync(
            $"/api/v1/projects/{project.Id}/issues/{issue.Id}/attachments/{attachment.Id}/content");
        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            contentResponse.StatusCode);

        var listResponse = await api.Client.GetFromJsonAsync<IssueAttachmentResponse[]>(
            $"/api/v1/projects/{project.Id}/issues/{issue.Id}/attachments");
        Assert.NotNull(listResponse);
        Assert.Equal(attachment.Id, Assert.Single(listResponse).Id);
    }
}
