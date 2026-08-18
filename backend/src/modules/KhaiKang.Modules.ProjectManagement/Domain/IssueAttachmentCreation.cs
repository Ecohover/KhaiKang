namespace KhaiKang.Modules.ProjectManagement.Domain;

public sealed class IssueAttachmentCreation
{
    public required Guid Id { get; init; }

    public required Guid IssueId { get; init; }

    public required string OriginalFileName { get; init; }

    public required string StorageProvider { get; init; }

    public required string StorageKey { get; init; }

    public required string ContentType { get; init; }

    public required long FileSize { get; init; }

    public required string FileHash { get; init; }
}
