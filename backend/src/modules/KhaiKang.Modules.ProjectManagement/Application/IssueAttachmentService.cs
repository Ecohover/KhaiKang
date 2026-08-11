using System.Security.Cryptography;
using KhaiKang.CommonUtils.Storage;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Domain;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KhaiKang.Modules.ProjectManagement.Application;

public sealed class IssueAttachmentService(
    ProjectManagementDbContext dbContext,
    IAccountDirectory accountDirectory,
    IFileStorage fileStorage,
    IOptions<FileStorageOptions> storageOptions,
    TimeProvider timeProvider)
{
    private readonly FileStorageOptions options = storageOptions.Value;

    public async Task<IReadOnlyList<IssueAttachmentResponse>?> ListAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(projectId, accountId, ProjectManagementConstants.IssueReadPermission, cancellationToken) ||
            !await IssueExistsAsync(projectId, issueId, cancellationToken))
        {
            return null;
        }

        var attachments = await dbContext.IssueAttachments.AsNoTracking()
            .Where(item => item.IssueId == issueId && !item.IsDeleted)
            .ToArrayAsync(cancellationToken);
        return await ToResponsesAsync(
            attachments.OrderByDescending(item => item.CreatedAt).ToArray(),
            cancellationToken);
    }

    public async Task<IssueAttachmentMutationResult> UploadAsync(
        Guid projectId,
        Guid issueId,
        Guid accountId,
        string fileName,
        string? contentType,
        long fileSize,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(projectId, accountId, ProjectManagementConstants.IssueAttachmentUploadPermission, cancellationToken))
        {
            return new(IssueAttachmentOutcome.Forbidden);
        }

        var issue = await dbContext.Issues
            .Include(item => item.Project)
            .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == issueId, cancellationToken);
        if (issue is null) return new(IssueAttachmentOutcome.NotFound);
        if (issue.Project.Status == ProjectStatus.Inactive) return new(IssueAttachmentOutcome.ProjectInactive);
        if (fileSize <= 0 || string.IsNullOrWhiteSpace(fileName)) return new(IssueAttachmentOutcome.InvalidFile);
        if (fileSize > options.MaxFileSizeBytes) return new(IssueAttachmentOutcome.FileTooLarge);

        var safeFileName = SanitizeFileName(fileName);
        var attachmentId = Guid.NewGuid();
        var storageKey = $"projects/{projectId}/issues/{issueId}/{attachmentId}";
        await using var bufferedContent = new MemoryStream((int)Math.Min(fileSize, int.MaxValue));
        await content.CopyToAsync(bufferedContent, cancellationToken);
        if (bufferedContent.Length != fileSize || bufferedContent.Length > options.MaxFileSizeBytes)
        {
            return new(IssueAttachmentOutcome.FileTooLarge);
        }

        var fileHash = Convert.ToHexStringLower(SHA256.HashData(bufferedContent.GetBuffer().AsSpan(0, (int)bufferedContent.Length)));
        bufferedContent.Position = 0;
        await fileStorage.WriteAsync(storageKey, bufferedContent, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var attachment = new IssueAttachment(
            attachmentId,
            issueId,
            accountId,
            safeFileName,
            fileStorage.Provider,
            storageKey,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            bufferedContent.Length,
            fileHash,
            now);
        dbContext.IssueAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(
            IssueAttachmentOutcome.Succeeded,
            (await ToResponsesAsync([attachment], cancellationToken))[0]);
    }

    public async Task<IssueAttachmentContentResult> OpenContentAsync(
        Guid projectId,
        Guid issueId,
        Guid attachmentId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(projectId, accountId, ProjectManagementConstants.IssueReadPermission, cancellationToken))
        {
            return new(IssueAttachmentOutcome.Forbidden);
        }

        var attachment = await dbContext.IssueAttachments.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId && item.IssueId == issueId &&
                    item.Issue.ProjectId == projectId && !item.IsDeleted,
                cancellationToken);
        if (attachment is null) return new(IssueAttachmentOutcome.NotFound);
        if (!string.Equals(attachment.StorageProvider, fileStorage.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return new(IssueAttachmentOutcome.StorageUnavailable);
        }

        var stream = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return stream is null
            ? new(IssueAttachmentOutcome.StorageUnavailable)
            : new(IssueAttachmentOutcome.Succeeded, stream, attachment.ContentType, attachment.OriginalFileName);
    }

    public async Task<IssueAttachmentMutationResult> DeleteAsync(
        Guid projectId,
        Guid issueId,
        Guid attachmentId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(projectId, accountId, ProjectManagementConstants.IssueAttachmentDeletePermission, cancellationToken))
        {
            return new(IssueAttachmentOutcome.Forbidden);
        }

        var attachment = await dbContext.IssueAttachments
            .Include(item => item.Issue)
            .ThenInclude(issue => issue.Project)
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId && item.IssueId == issueId && item.Issue.ProjectId == projectId,
                cancellationToken);
        if (attachment is null || attachment.IsDeleted) return new(IssueAttachmentOutcome.NotFound);
        if (attachment.Issue.Project.Status == ProjectStatus.Inactive) return new(IssueAttachmentOutcome.ProjectInactive);

        attachment.MarkDeleted(accountId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(IssueAttachmentOutcome.Succeeded);
    }

    private Task<bool> IssueExistsAsync(Guid projectId, Guid issueId, CancellationToken cancellationToken) =>
        dbContext.Issues.AnyAsync(item => item.ProjectId == projectId && item.Id == issueId, cancellationToken);

    private Task<bool> HasPermissionAsync(Guid projectId, Guid accountId, string permissionCode, CancellationToken cancellationToken) =>
        dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId && member.AccountId == accountId && member.Status == ProjectMemberStatus.Active &&
                member.Roles.Any(mapping => mapping.ProjectRole.Permissions.Any(permission => permission.Permission.Code == permissionCode)),
            cancellationToken);

    private async Task<IReadOnlyList<IssueAttachmentResponse>> ToResponsesAsync(
        IReadOnlyList<IssueAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var accounts = await accountDirectory.GetByIdsAsync(
            attachments.Select(item => item.UploadedByAccountId).Distinct().ToArray(),
            cancellationToken);
        return attachments.Select(item => new IssueAttachmentResponse(
            item.Id,
            item.IssueId,
            item.OriginalFileName,
            item.ContentType,
            item.FileSize,
            item.FileHash,
            item.UploadedByAccountId,
            accounts.TryGetValue(item.UploadedByAccountId, out var account) ? account.Username : string.Empty,
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
