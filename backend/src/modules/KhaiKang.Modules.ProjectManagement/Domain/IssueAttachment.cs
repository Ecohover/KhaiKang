namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueAttachment : AuditableEntity
{
    private IssueAttachment() { }

    private IssueAttachment(IssueAttachmentCreation creation, ChangeContext context)
    {
        Id = creation.Id;
        IssueId = creation.IssueId;
        UploadedByAccountId = context.ActorAccountId;
        OriginalFileName = creation.OriginalFileName;
        StorageProvider = creation.StorageProvider;
        StorageKey = creation.StorageKey;
        ContentType = creation.ContentType;
        FileSize = creation.FileSize;
        FileHash = creation.FileHash;
        InitializeAudit(context);
    }

    public static IssueAttachment Create(
        IssueAttachmentCreation creation,
        ChangeContext context)
    {
        return new IssueAttachment(creation, context);
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
    public void MarkDeleted(ChangeContext context)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = context.OccurredAt;
        RecordChange(context);
    }
}
