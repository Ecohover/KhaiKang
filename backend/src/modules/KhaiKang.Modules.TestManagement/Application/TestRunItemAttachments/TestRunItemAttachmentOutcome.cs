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
