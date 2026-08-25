namespace KhaiKang.Api.Configuration;

public sealed class DataProtectionKeyOptions
{
    public const string SectionName = "DataProtection";

    public string? KeysDirectory { get; init; }
}
