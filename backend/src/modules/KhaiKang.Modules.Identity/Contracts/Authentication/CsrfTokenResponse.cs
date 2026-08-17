namespace KhaiKang.Modules.Identity.Contracts;

public sealed record CsrfTokenResponse
{
    public required string Token { get; init; }
}
