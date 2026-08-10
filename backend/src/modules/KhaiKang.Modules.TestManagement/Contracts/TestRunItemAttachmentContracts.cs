namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record TestRunItemAttachmentResponse(
    Guid Id,
    Guid TestRunItemId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string FileHash,
    Guid UploadedByAccountId,
    string UploadedByUsername,
    DateTimeOffset CreatedAt);
