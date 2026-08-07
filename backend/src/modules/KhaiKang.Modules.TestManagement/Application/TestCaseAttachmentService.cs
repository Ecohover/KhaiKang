using System.Security.Cryptography;
using KhaiKang.CommonUtils.Storage;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Domain;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KhaiKang.Modules.TestManagement.Application;

public sealed class TestCaseAttachmentService(
    TestManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    IFileStorage fileStorage,
    IOptions<FileStorageOptions> storageOptions,
    TimeProvider timeProvider)
{
    private readonly FileStorageOptions options = storageOptions.Value;

    public async Task<IReadOnlyList<TestCaseAttachmentResponse>?> ListAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(workspaceId, accountId, cancellationToken) ||
            !await CaseExistsAsync(workspaceId, caseId, cancellationToken))
        {
            return null;
        }

        var attachments = await dbContext.CaseAttachments.AsNoTracking()
            .Where(item => item.TestCaseId == caseId && !item.IsDeleted)
            .ToArrayAsync(cancellationToken);
        return await ToResponsesAsync(
            attachments.OrderByDescending(item => item.CreatedAt).ToArray(),
            cancellationToken);
    }

    public async Task<TestCaseAttachmentMutationResult> UploadAsync(
        Guid workspaceId,
        Guid caseId,
        Guid accountId,
        string fileName,
        string? contentType,
        long fileSize,
        Stream content,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null) return new(TestCaseAttachmentOutcome.NotFound);
        if (access.Role is not ("owner" or "manager")) return new(TestCaseAttachmentOutcome.Forbidden);
        if (access.Workspace.Status != "active") return new(TestCaseAttachmentOutcome.WorkspaceInactive);
        if (!await CaseExistsAsync(workspaceId, caseId, cancellationToken)) return new(TestCaseAttachmentOutcome.NotFound);
        if (fileSize <= 0 || string.IsNullOrWhiteSpace(fileName)) return new(TestCaseAttachmentOutcome.InvalidFile);
        if (fileSize > options.MaxFileSizeBytes) return new(TestCaseAttachmentOutcome.FileTooLarge);

        var safeFileName = SanitizeFileName(fileName);
        var attachmentId = Guid.NewGuid();
        var storageKey = $"test-workspaces/{workspaceId}/cases/{caseId}/{attachmentId}";
        await using var bufferedContent = new MemoryStream((int)Math.Min(fileSize, int.MaxValue));
        await content.CopyToAsync(bufferedContent, cancellationToken);
        if (bufferedContent.Length != fileSize || bufferedContent.Length > options.MaxFileSizeBytes)
        {
            return new(TestCaseAttachmentOutcome.FileTooLarge);
        }

        var fileHash = Convert.ToHexStringLower(SHA256.HashData(
            bufferedContent.GetBuffer().AsSpan(0, (int)bufferedContent.Length)));
        bufferedContent.Position = 0;
        await fileStorage.WriteAsync(storageKey, bufferedContent, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var attachment = new TestCaseAttachment(
            attachmentId,
            caseId,
            accountId,
            safeFileName,
            fileStorage.Provider,
            storageKey,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            bufferedContent.Length,
            fileHash,
            now);
        dbContext.CaseAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(
            TestCaseAttachmentOutcome.Succeeded,
            (await ToResponsesAsync([attachment], cancellationToken))[0]);
    }

    public async Task<TestCaseAttachmentContentResult> OpenContentAsync(
        Guid workspaceId,
        Guid caseId,
        Guid attachmentId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(workspaceId, accountId, cancellationToken))
            return new(TestCaseAttachmentOutcome.Forbidden);

        var attachment = await dbContext.CaseAttachments.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == attachmentId && item.TestCaseId == caseId &&
                item.TestCase.TestWorkspaceId == workspaceId && !item.IsDeleted,
                cancellationToken);
        if (attachment is null) return new(TestCaseAttachmentOutcome.NotFound);
        if (!string.Equals(attachment.StorageProvider, fileStorage.Provider, StringComparison.OrdinalIgnoreCase))
            return new(TestCaseAttachmentOutcome.StorageUnavailable);

        var stream = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return stream is null
            ? new(TestCaseAttachmentOutcome.StorageUnavailable)
            : new(TestCaseAttachmentOutcome.Succeeded, stream, attachment.ContentType, attachment.OriginalFileName);
    }

    public async Task<TestCaseAttachmentMutationResult> DeleteAsync(
        Guid workspaceId,
        Guid caseId,
        Guid attachmentId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(workspaceId, accountId, cancellationToken);
        if (access is null) return new(TestCaseAttachmentOutcome.NotFound);
        if (access.Role is not ("owner" or "manager")) return new(TestCaseAttachmentOutcome.Forbidden);
        if (access.Workspace.Status != "active") return new(TestCaseAttachmentOutcome.WorkspaceInactive);

        var attachment = await dbContext.CaseAttachments.SingleOrDefaultAsync(item =>
            item.Id == attachmentId && item.TestCaseId == caseId &&
            item.TestCase.TestWorkspaceId == workspaceId && !item.IsDeleted,
            cancellationToken);
        if (attachment is null) return new(TestCaseAttachmentOutcome.NotFound);
        attachment.MarkDeleted(accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(TestCaseAttachmentOutcome.Succeeded);
    }

    private Task<TestWorkspaceMember?> AccessAsync(
        Guid workspaceId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.Members.Include(item => item.Workspace).SingleOrDefaultAsync(item =>
            item.TestWorkspaceId == workspaceId && item.AccountId == accountId && item.Status == "active",
            cancellationToken);

    private Task<bool> HasAccessAsync(Guid workspaceId, Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(item =>
            item.TestWorkspaceId == workspaceId && item.AccountId == accountId && item.Status == "active",
            cancellationToken);

    private Task<bool> CaseExistsAsync(Guid workspaceId, Guid caseId, CancellationToken cancellationToken) =>
        dbContext.Cases.AnyAsync(item => item.Id == caseId && item.TestWorkspaceId == workspaceId, cancellationToken);

    private async Task<IReadOnlyList<TestCaseAttachmentResponse>> ToResponsesAsync(
        IReadOnlyList<TestCaseAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var accounts = await accountDirectory.GetByIdsAsync(
            attachments.Select(item => item.UploadedByAccountId).Distinct().ToArray(),
            cancellationToken);
        return attachments.Select(item => new TestCaseAttachmentResponse(
            item.Id,
            item.TestCaseId,
            item.OriginalFileName,
            item.ContentType,
            item.FileSize,
            item.FileHash,
            item.UploadedByAccountId,
            accounts.GetValueOrDefault(item.UploadedByAccountId)?.Username ?? string.Empty,
            item.CreatedAt)).ToArray();
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName).Trim();
        name = string.Concat(name.Where(character => !char.IsControl(character)));
        if (string.IsNullOrWhiteSpace(name)) name = "attachment";
        return name.Length <= 255 ? name : name[..255];
    }
}
