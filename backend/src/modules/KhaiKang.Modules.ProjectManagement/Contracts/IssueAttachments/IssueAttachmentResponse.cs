namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueAttachmentResponse
{
    public required Guid Id { get; init; }

    public required Guid IssueId { get; init; }

    public required string OriginalFileName { get; init; }

    public required string ContentType { get; init; }

    public required long FileSize { get; init; }

    public required string FileHash { get; init; }

    public required Guid UploadedByAccountId { get; init; }

    public required string UploadedByUsername { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
