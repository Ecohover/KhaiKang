namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueAttachment
{
    private IssueAttachment() { }

    public IssueAttachment(
        Guid id,
        Guid issueId,
        Guid uploadedByAccountId,
        string originalFileName,
        string storageProvider,
        string storageKey,
        string contentType,
        long fileSize,
        string fileHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        IssueId = issueId;
        UploadedByAccountId = uploadedByAccountId;
        OriginalFileName = originalFileName;
        StorageProvider = storageProvider;
        StorageKey = storageKey;
        ContentType = contentType;
        FileSize = fileSize;
        FileHash = fileHash;
        CreatedAt = createdAt;
        CreatedByAccountId = uploadedByAccountId;
        UpdatedAt = createdAt;
        UpdatedByAccountId = uploadedByAccountId;
    }

    public Guid Id { get; private set; }
    public Guid IssueId { get; private set; }
    public Issue Issue { get; private set; } = null!;
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

    public void MarkDeleted(Guid actorAccountId, DateTimeOffset deletedAt)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = deletedAt;
        UpdatedAt = deletedAt;
        UpdatedByAccountId = actorAccountId;
        Version++;
    }
}
