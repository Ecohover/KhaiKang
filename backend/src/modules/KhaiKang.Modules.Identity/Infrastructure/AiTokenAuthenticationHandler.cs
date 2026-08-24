using System.Security.Claims;
using System.Text.Encodings.Web;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KhaiKang.Modules.Identity.Infrastructure;

public sealed class AiTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IdentityDbContext dbContext,
    IAiTokenProtector tokenProtector)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var plaintext = authorization["Bearer ".Length..].Trim();
        var tokenId = tokenProtector.ReadTokenId(plaintext);
        if (tokenId is null)
        {
            return AuthenticateResult.Fail("Invalid bearer token.");
        }

        var token = await dbContext.AiAccountTokens
            .AsNoTracking()
            .Include(item => item.AiAccount)
            .ThenInclude(account => account.OwnerAccount)
            .SingleOrDefaultAsync(item => item.Id == tokenId.Value, Context.RequestAborted);
        if (token is null ||
            !token.IsActive ||
            token.AiAccount.AccountType != AccountType.AiAgent ||
            token.AiAccount.Status != AccountStatus.Active ||
            !token.AiAccount.AiApiEnabled ||
            token.AiAccount.OwnerAccount is not
            {
                AccountType: AccountType.Human,
                Status: AccountStatus.Active,
            } owner ||
            !tokenProtector.Verify(plaintext, token.LookupVerifier))
        {
            return AuthenticateResult.Fail("Invalid bearer token.");
        }

        var claims = new Claim[]
        {
            new(ClaimTypes.NameIdentifier, token.AiAccount.Id.ToString()),
            new(ClaimTypes.Name, token.AiAccount.Username),
            new(IdentityConstants.AccountTypeClaim, "ai_agent"),
            new(
                IdentityConstants.FixedRoleClaim,
                AiFixedRoleCodes.ToCode(token.AiAccount.FixedRole!.Value)),
            new(IdentityConstants.OwnerAccountIdClaim, owner.Id.ToString()),
            new("owner_display_name", owner.Username),
        };
        var identity = new ClaimsIdentity(claims, IdentityConstants.AiTokenAuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
