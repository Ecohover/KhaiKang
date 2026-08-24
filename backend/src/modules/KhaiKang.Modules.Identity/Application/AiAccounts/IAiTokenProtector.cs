namespace KhaiKang.Modules.Identity.Application.AiAccounts;

public interface IAiTokenProtector
{
    IssuedAiToken Issue(Guid tokenId);

    Guid? ReadTokenId(string plaintext);

    bool Verify(string plaintext, string expectedVerifier);

    string Reveal(string protectedSecret);
}
