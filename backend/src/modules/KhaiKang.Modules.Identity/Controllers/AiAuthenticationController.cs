using System.Security.Claims;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Contracts.AiAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/ai-auth")]
[Authorize(AuthenticationSchemes = IdentityConstants.AiTokenAuthenticationScheme)]
public sealed class AiAuthenticationController : ControllerBase
{
    [HttpGet("me", Name = "GetCurrentAiBearerIdentity")]
    [ProducesResponseType<AiBearerIdentityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrent()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) ||
            !Guid.TryParse(User.FindFirstValue(IdentityConstants.OwnerAccountIdClaim), out var ownerId))
        {
            return Unauthorized();
        }

        return Ok(new AiBearerIdentityResponse
        {
            Id = accountId,
            Username = User.FindFirstValue(ClaimTypes.Name)!,
            AccountType = User.FindFirstValue(IdentityConstants.AccountTypeClaim)!,
            FixedRole = User.FindFirstValue(IdentityConstants.FixedRoleClaim)!,
            OwnerAccountId = ownerId,
            OwnerDisplayName = User.FindFirstValue("owner_display_name")!,
        });
    }
}
