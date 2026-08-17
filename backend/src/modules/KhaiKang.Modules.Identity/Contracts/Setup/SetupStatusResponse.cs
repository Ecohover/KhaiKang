namespace KhaiKang.Modules.Identity.Contracts;

public sealed record SetupStatusResponse
{
    public required bool RequiresInitialization { get; init; }
}
