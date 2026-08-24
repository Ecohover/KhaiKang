using System.Security.Cryptography;
using System.Text;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Configuration;
using KhaiKang.Modules.Identity.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class AiTokenProtector : IAiTokenProtector
{
    private const string TokenPrefix = "kkai";
    private const string ProtectionVersion = "v1";
    private readonly IDataProtector _protector;

    public AiTokenProtector(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<IdentityOptions> options)
    {
        _protector = dataProtectionProvider.CreateProtector(
            options.Value.AiTokenProtectionPurpose,
            ProtectionVersion);
    }

    public IssuedAiToken Issue(Guid tokenId)
    {
        var secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var plaintext = $"{TokenPrefix}_{tokenId:N}_{secret}";
        return new IssuedAiToken
        {
            Plaintext = plaintext,
            Material = new AiTokenMaterial
            {
                ProtectedSecret = _protector.Protect(plaintext),
                LookupVerifier = ComputeVerifier(plaintext),
                ProtectionVersion = ProtectionVersion,
            },
        };
    }

    public Guid? ReadTokenId(string plaintext)
    {
        var parts = plaintext.Split('_', 3, StringSplitOptions.None);
        return parts.Length == 3 &&
            parts[0] == TokenPrefix &&
            Guid.TryParseExact(parts[1], "N", out var tokenId)
                ? tokenId
                : null;
    }

    public bool Verify(string plaintext, string expectedVerifier)
    {
        try
        {
            var actual = Convert.FromHexString(ComputeVerifier(plaintext));
            var expected = Convert.FromHexString(expectedVerifier);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string Reveal(string protectedSecret) => _protector.Unprotect(protectedSecret);

    private static string ComputeVerifier(string plaintext)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
    }
}
