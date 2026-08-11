using KhaiKang.Modules.ProjectManagement.Domain;

namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record IssueDirectoryEntry(
    Guid Id,
    Guid ProjectId,
    string ProjectCode,
    ProjectStatus ProjectStatus,
    int IssueNo,
    string Key,
    string Title,
    string TypeCode,
    string StatusCode);
