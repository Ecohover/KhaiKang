namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record UpdateTestRunStatusRequest
{
    public UpdateTestRunStatusRequest(string status, string? summary, int version)
    {
        Status = status;
        Summary = summary;
        Version = version;
    }

    public string Status { get; }

    public string? Summary { get; }

    public int Version { get; }
}
