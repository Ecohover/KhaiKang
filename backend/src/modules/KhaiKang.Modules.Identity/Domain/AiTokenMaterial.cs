namespace KhaiKang.Modules.Identity.Domain;

public sealed record AiTokenMaterial
{
    public required string ProtectedSecret { get; init; }
    public required string LookupVerifier { get; init; }
    public required string ProtectionVersion { get; init; }
}
