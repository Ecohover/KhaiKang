namespace KhaiKang.Modules.TestManagement.Contracts;

public sealed record RecordTestResultRequest
{
    public RecordTestResultRequest(string status, string? actualResult, int version)
    {
        Status = status;
        ActualResult = actualResult;
        Version = version;
    }

    public string Status { get; }

    public string? ActualResult { get; }

    public int Version { get; }
}
