using System.Security.Cryptography;
using KhaiKang.CommonUtils.Storage;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TestRunItemAttachmentService(
    TestManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    IFileStorage fileStorage,
    IOptions<FileStorageOptions> storageOptions,
    TimeProvider timeProvider)
{
    private readonly FileStorageOptions options = storageOptions.Value;

    public async Task<IReadOnlyList<TestRunItemAttachmentResponse>?> ListAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(workspaceId, accountId, cancellationToken) ||
            await RunItemAsync(workspaceId, runId, itemId, cancellationToken) is null)
        {
            return null;
        }

        var attachments = await dbContext.RunItemAttachments.AsNoTracking()
            .Where(item => item.TestRunItemId == itemId && !item.IsDeleted)
            .ToArrayAsync(cancellationToken);
        return await ToResponsesAsync(
            attachments.OrderByDescending(item => item.CreatedAt).ToArray(),
            cancellationToken);
    }

    public async Task<TestRunItemAttachmentMutationResult> UploadAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        Guid accountId,
        string fileName,
        string? contentType,
        long fileSize,
        Stream content,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.NotFound);
        if (!CanExecute(access.Role))
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.Forbidden);
        if (access.Workspace.Status != TestAssetStatus.Active)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.WorkspaceInactive);

        var runItem = await RunItemAsync(workspaceId, runId, itemId, cancellationToken);
        if (runItem is null)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.NotFound);
        if (runItem.RunStatus != TestRunStatus.InProgress)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.RunNotInProgress);
        if (fileSize <= 0 || string.IsNullOrWhiteSpace(fileName))
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.InvalidFile);
        if (fileSize > options.MaxFileSizeBytes)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.FileTooLarge);

        var safeFileName = SanitizeFileName(fileName);
        var attachmentId = Guid.NewGuid();
        var storageKey = $"test-workspaces/{workspaceId}/runs/{runId}/items/{itemId}/{attachmentId}";
        await using var bufferedContent = new MemoryStream((int)Math.Min(fileSize, int.MaxValue));
        await content.CopyToAsync(bufferedContent, cancellationToken);
        if (bufferedContent.Length != fileSize || bufferedContent.Length > options.MaxFileSizeBytes)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.FileTooLarge);

        var fileHash = Convert.ToHexStringLower(SHA256.HashData(
            bufferedContent.GetBuffer().AsSpan(0, (int)bufferedContent.Length)));
        bufferedContent.Position = 0;
        await fileStorage.WriteAsync(storageKey, bufferedContent, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var attachment = new TestRunItemAttachment(
            attachmentId,
            itemId,
            accountId,
            safeFileName,
            fileStorage.Provider,
            storageKey,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            bufferedContent.Length,
            fileHash,
            now);
        dbContext.RunItemAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestRunItemAttachmentMutationResult.Uploaded(
            (await ToResponsesAsync([attachment], cancellationToken))[0]);
    }

    public async Task<TestRunItemAttachmentContentResult> OpenContentAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        Guid attachmentId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(workspaceId, accountId, cancellationToken))
            return TestRunItemAttachmentContentResult.Failure(TestRunItemAttachmentOutcome.Forbidden);
        if (await RunItemAsync(workspaceId, runId, itemId, cancellationToken) is null)
            return TestRunItemAttachmentContentResult.Failure(TestRunItemAttachmentOutcome.NotFound);

        var attachment = await dbContext.RunItemAttachments.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == attachmentId && item.TestRunItemId == itemId &&
                !item.IsDeleted,
                cancellationToken);
        if (attachment is null)
            return TestRunItemAttachmentContentResult.Failure(TestRunItemAttachmentOutcome.NotFound);
        if (!string.Equals(attachment.StorageProvider, fileStorage.Provider, StringComparison.OrdinalIgnoreCase))
            return TestRunItemAttachmentContentResult.Failure(TestRunItemAttachmentOutcome.StorageUnavailable);

        var stream = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return stream is null
            ? TestRunItemAttachmentContentResult.Failure(TestRunItemAttachmentOutcome.StorageUnavailable)
            : TestRunItemAttachmentContentResult.Success(
                stream,
                attachment.ContentType,
                attachment.OriginalFileName);
    }

    public async Task<TestRunItemAttachmentMutationResult> DeleteAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        Guid attachmentId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.NotFound);
        if (!CanExecute(access.Role))
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.Forbidden);
        if (access.Workspace.Status != TestAssetStatus.Active)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.WorkspaceInactive);

        var runItem = await RunItemAsync(workspaceId, runId, itemId, cancellationToken);
        if (runItem is null)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.NotFound);
        if (runItem.RunStatus != TestRunStatus.InProgress)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.RunNotInProgress);

        var attachment = await dbContext.RunItemAttachments.SingleOrDefaultAsync(item =>
            item.Id == attachmentId && item.TestRunItemId == itemId && !item.IsDeleted,
            cancellationToken);
        if (attachment is null)
            return TestRunItemAttachmentMutationResult.Failure(TestRunItemAttachmentOutcome.NotFound);
        attachment.MarkDeleted(accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return TestRunItemAttachmentMutationResult.Deleted();
    }

    private Task<TestWorkspaceMember?> AccessAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.Members.Include(item => item.Workspace).SingleOrDefaultAsync(item =>
            item.TestWorkspaceId == workspaceId && item.AccountId == accountId &&
            item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);

    private Task<bool> HasAccessAsync(Guid workspaceId, Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(item =>
            item.TestWorkspaceId == workspaceId && item.AccountId == accountId &&
            item.Status == TestWorkspaceMemberStatus.Active,
            cancellationToken);

    private Task<RunItemAccess?> RunItemAsync(
        Guid workspaceId,
        Guid runId,
        Guid itemId,
        CancellationToken cancellationToken) =>
        (from item in dbContext.RunItems.AsNoTracking()
         join run in dbContext.Runs.AsNoTracking() on item.TestRunId equals run.Id
         join plan in dbContext.Plans.AsNoTracking() on run.TestPlanId equals plan.Id
         where item.Id == itemId && run.Id == runId && plan.TestWorkspaceId == workspaceId
         select new RunItemAccess(run.Status))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<TestRunItemAttachmentResponse>> ToResponsesAsync(
        IReadOnlyList<TestRunItemAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var accounts = await accountDirectory.GetByIdsAsync(
            attachments.Select(item => item.UploadedByAccountId).Distinct().ToArray(),
            cancellationToken);
        return attachments.Select(item => new TestRunItemAttachmentResponse
        {
            Id = item.Id,
            TestRunItemId = item.TestRunItemId,
            OriginalFileName = item.OriginalFileName,
            ContentType = item.ContentType,
            FileSize = item.FileSize,
            FileHash = item.FileHash,
            UploadedByAccountId = item.UploadedByAccountId,
            UploadedByUsername = accounts.GetValueOrDefault(item.UploadedByAccountId)?.Username ?? string.Empty,
            CreatedAt = item.CreatedAt,
        }).ToArray();
    }

    private static bool CanExecute(TestWorkspaceRole role) =>
        role is TestWorkspaceRole.Owner or TestWorkspaceRole.Manager or TestWorkspaceRole.Tester;

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName).Trim();
        name = string.Concat(name.Where(character => !char.IsControl(character)));
        if (string.IsNullOrWhiteSpace(name)) name = "attachment";
        return name.Length <= 255 ? name : name[..255];
    }

    private sealed record RunItemAccess(TestRunStatus RunStatus);
}
