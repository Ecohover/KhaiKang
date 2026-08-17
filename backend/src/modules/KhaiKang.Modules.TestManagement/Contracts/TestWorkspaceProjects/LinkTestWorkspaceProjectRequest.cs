namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record LinkTestWorkspaceProjectRequest
{
    public LinkTestWorkspaceProjectRequest(Guid projectId)
    {
        ProjectId = projectId;
    }

    public Guid ProjectId { get; }
}
