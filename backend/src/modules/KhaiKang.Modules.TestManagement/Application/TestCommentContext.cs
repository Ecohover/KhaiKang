namespace KhaiKang.Modules.TestManagement.Application;

internal sealed record TestCommentContext(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid TestIssueId);
