namespace KhaiKang.Modules.ProjectManagement.Contracts;

public sealed record UpdateIssueStatusRequest
{
    public UpdateIssueStatusRequest(string statusCode, int version)
    {
        StatusCode = statusCode;
        Version = version;
    }

    public string StatusCode { get; }

    public int Version { get; }
}
