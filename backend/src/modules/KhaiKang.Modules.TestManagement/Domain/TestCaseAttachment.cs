namespace KhaiKang.Modules.TestManagement.Domain;

public sealed class TestCaseAttachment
{
    private TestCaseAttachment() { }
    public TestCaseAttachment(Guid id, Guid testCaseId, Guid accountId, string fileName, string provider, string storageKey, string contentType, long fileSize, string fileHash, DateTimeOffset now)
    {
        Id = id; TestCaseId = testCaseId; UploadedByAccountId = accountId; OriginalFileName = fileName;
        StorageProvider = provider; StorageKey = storageKey; ContentType = contentType; FileSize = fileSize;
        FileHash = fileHash; CreatedAt = now; CreatedByAccountId = accountId; UpdatedAt = now; UpdatedByAccountId = accountId;
    }
    public Guid Id { get; private set; }
    public Guid TestCaseId { get; private set; }
    public TestCase TestCase { get; private set; } = null!;
    public Guid UploadedByAccountId { get; private set; }
    public string OriginalFileName { get; private set; } = null!;
    public string StorageProvider { get; private set; } = null!;
    public string StorageKey { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string FileHash { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedByAccountId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public int Version { get; private set; } = 1;
    public void MarkDeleted(Guid accountId, DateTimeOffset now) { IsDeleted = true; DeletedAt = now; UpdatedAt = now; UpdatedByAccountId = accountId; Version++; }
}
