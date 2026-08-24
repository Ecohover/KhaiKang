using KhaiKang.Modules.Identity.Domain;

namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public sealed record IssuedAiToken
{
    public required string Plaintext { get; init; }
    public required AiTokenMaterial Material { get; init; }
}
