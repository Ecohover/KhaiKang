namespace KhaiKang.Api.Contracts;

public sealed record SystemInfoResponse(
    string ServiceName,
    string Environment,
    DateTimeOffset ServerTime);
