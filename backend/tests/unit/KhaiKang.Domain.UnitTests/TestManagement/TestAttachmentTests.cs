using KhaiKang.Modules.TestManagement.Domain;

namespace KhaiKang.Domain.UnitTests;

public sealed class TestAttachmentTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 11, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CaseAttachment_CapturesStorageMetadataAndDeletionAudit()
    {
        var actorId = Guid.NewGuid();
        var attachment = new TestCaseAttachment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorId,
            "case.png",
            "local",
            "cases/storage-key",
            "image/png",
            256,
            "case-hash",
            CreatedAt);
        var deletedBy = Guid.NewGuid();
        var deletedAt = CreatedAt.AddMinutes(10);

        attachment.MarkDeleted(deletedBy, deletedAt);

        Assert.Equal("case.png", attachment.OriginalFileName);
        Assert.Equal("local", attachment.StorageProvider);
        Assert.Equal("cases/storage-key", attachment.StorageKey);
        Assert.True(attachment.IsDeleted);
        Assert.Equal(deletedAt, attachment.DeletedAt);
        Assert.Equal(deletedBy, attachment.UpdatedByAccountId);
        Assert.Equal(2, attachment.Version);
    }

    [Fact]
    public void RunItemAttachment_CapturesStorageMetadataAndDeletionAudit()
    {
        var actorId = Guid.NewGuid();
        var attachment = new TestRunItemAttachment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorId,
            "run.log",
            "local",
            "runs/storage-key",
            "text/plain",
            512,
            "run-hash",
            CreatedAt);
        var deletedBy = Guid.NewGuid();
        var deletedAt = CreatedAt.AddMinutes(10);

        attachment.MarkDeleted(deletedBy, deletedAt);

        Assert.Equal("run.log", attachment.OriginalFileName);
        Assert.Equal("local", attachment.StorageProvider);
        Assert.Equal("runs/storage-key", attachment.StorageKey);
        Assert.True(attachment.IsDeleted);
        Assert.Equal(deletedAt, attachment.DeletedAt);
        Assert.Equal(deletedBy, attachment.UpdatedByAccountId);
        Assert.Equal(2, attachment.Version);
    }
}
