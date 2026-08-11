using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class IssueAttachmentTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_CapturesStorageMetadataAndUploaderAudit()
    {
        var issueId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var attachment = CreateAttachment(issueId, uploaderId);

        Assert.Equal(issueId, attachment.IssueId);
        Assert.Equal(uploaderId, attachment.UploadedByAccountId);
        Assert.Equal("evidence.png", attachment.OriginalFileName);
        Assert.Equal("local", attachment.StorageProvider);
        Assert.Equal("issues/storage-key", attachment.StorageKey);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(128, attachment.FileSize);
        Assert.Equal("sha256", attachment.FileHash);
        Assert.False(attachment.IsDeleted);
        Assert.Equal(uploaderId, attachment.CreatedByAccountId);
        Assert.Equal(1, attachment.Version);
    }

    [Fact]
    public void MarkDeleted_MarksAttachmentOnceAndPreservesFirstDeletionAudit()
    {
        var attachment = CreateAttachment();
        var actorId = Guid.NewGuid();
        var deletedAt = CreatedAt.AddMinutes(5);

        attachment.MarkDeleted(new ChangeContext(actorId, deletedAt));
        attachment.MarkDeleted(new ChangeContext(Guid.NewGuid(), deletedAt.AddMinutes(5)));

        Assert.True(attachment.IsDeleted);
        Assert.Equal(deletedAt, attachment.DeletedAt);
        Assert.Equal(actorId, attachment.UpdatedByAccountId);
        Assert.Equal(deletedAt, attachment.UpdatedAt);
        Assert.Equal(2, attachment.Version);
    }

    private static IssueAttachment CreateAttachment(
        Guid? issueId = null,
        Guid? uploaderId = null)
    {
        return IssueAttachment.Create(
            new IssueAttachmentCreation
            {
                Id = Guid.NewGuid(),
                IssueId = issueId ?? Guid.NewGuid(),
                OriginalFileName = "evidence.png",
                StorageProvider = "local",
                StorageKey = "issues/storage-key",
                ContentType = "image/png",
                FileSize = 128,
                FileHash = "sha256",
            },
            new ChangeContext(uploaderId ?? Guid.NewGuid(), CreatedAt));
    }
}
