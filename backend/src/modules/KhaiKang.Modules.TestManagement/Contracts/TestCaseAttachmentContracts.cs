namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestCaseAttachmentResponse(
    Guid Id,
    Guid TestCaseId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string FileHash,
    Guid UploadedByAccountId,
    string UploadedByUsername,
    DateTimeOffset CreatedAt);
