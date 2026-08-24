using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record CreateAiAccountCommand
{
    public required Guid ActorAccountId { get; init; }
    public required string Username { get; init; }
    public required AiFixedRole FixedRole { get; init; }
}
