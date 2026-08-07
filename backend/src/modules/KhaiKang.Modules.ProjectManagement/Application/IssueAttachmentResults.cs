using KhaiKang.Modules.ProjectManagement.Contracts;

namespace KhaiKang.Modules.ProjectManagement.Application;

public enum IssueAttachmentOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    ProjectInactive,
    FileTooLarge,
    InvalidFile,
    StorageUnavailable,
}

public sealed record IssueAttachmentMutationResult(
    IssueAttachmentOutcome Outcome,
    IssueAttachmentResponse? Attachment = null);

public sealed record IssueAttachmentContentResult(
    IssueAttachmentOutcome Outcome,
    Stream? Content = null,
    string? ContentType = null,
    string? FileName = null);
