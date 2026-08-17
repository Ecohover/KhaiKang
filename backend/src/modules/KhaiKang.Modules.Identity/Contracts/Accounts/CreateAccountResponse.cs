namespace KhaiKang.Modules.Identity.Contracts;

public sealed record CreateAccountResponse
{
    public required AccountResponse Account { get; init; }

    public required string InitialPassword { get; init; }
}
