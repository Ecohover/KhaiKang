namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueAttachmentResponse(
    Guid Id,
    Guid IssueId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string FileHash,
    Guid UploadedByAccountId,
    string UploadedByUsername,
    DateTimeOffset CreatedAt);
