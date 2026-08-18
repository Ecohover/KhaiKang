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
