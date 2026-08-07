using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Modules.TestManagement.Application;

public enum TestCaseAttachmentOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    WorkspaceInactive,
    FileTooLarge,
    InvalidFile,
    StorageUnavailable,
}

public sealed record TestCaseAttachmentMutationResult(
    TestCaseAttachmentOutcome Outcome,
    TestCaseAttachmentResponse? Attachment = null);

public sealed record TestCaseAttachmentContentResult(
    TestCaseAttachmentOutcome Outcome,
    Stream? Content = null,
    string? ContentType = null,
    string? FileName = null);
