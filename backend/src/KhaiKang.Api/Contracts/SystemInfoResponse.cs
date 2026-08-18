namespace KhaiKang.Api.Contracts;

public sealed record SystemInfoResponse
{
    public required string ServiceName { get; init; }

    public required string Version { get; init; }

    public required string Environment { get; init; }

    public required DateTimeOffset ServerTime { get; init; }
}
