namespace KhaiKang.Modules.Identity.Contracts;

public sealed record UpdateAccountStatusRequest
{
    public required string Status { get; init; }

    public required int Version { get; init; }
}
