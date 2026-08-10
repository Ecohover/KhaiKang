using KhaiKang.Modules.TestManagement.Contracts;

namespace KhaiKang.Modules.TestManagement.Application;

public enum TestRunItemAttachmentOutcome
{
    Succeeded,
    NotFound,
    Forbidden,
    WorkspaceInactive,
    RunNotInProgress,
    FileTooLarge,
    InvalidFile,
    StorageUnavailable,
}

public sealed record TestRunItemAttachmentMutationResult(
    TestRunItemAttachmentOutcome Outcome,
    TestRunItemAttachmentResponse? Attachment = null);

public sealed record TestRunItemAttachmentContentResult(
    TestRunItemAttachmentOutcome Outcome,
    Stream? Content = null,
    string? ContentType = null,
    string? FileName = null);
