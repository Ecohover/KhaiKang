namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueDirectoryEntry(
    Guid Id,
    Guid ProjectId,
    string ProjectCode,
    string ProjectStatus,
    int IssueNo,
    string Key,
    string Title,
    string TypeCode,
    string StatusCode);
