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
