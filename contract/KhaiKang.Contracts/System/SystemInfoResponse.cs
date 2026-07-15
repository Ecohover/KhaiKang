namespace KhaiKang.Contracts.System;

public sealed record SystemInfoResponse(
    string ServiceName,
    string Environment,
    DateTimeOffset ServerTime);
