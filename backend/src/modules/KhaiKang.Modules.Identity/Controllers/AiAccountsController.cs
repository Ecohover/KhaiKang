using System.Security.Claims;
using System.Text.RegularExpressions;
using KhaiKang.Modules.Identity.Application;
using KhaiKang.Modules.Identity.Application.AiAccounts;
using KhaiKang.Modules.Identity.Contracts.AiAccounts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KhaiKang.Modules.Identity.Controllers;

[ApiController]
[Route("api/v1/ai-accounts")]
public sealed partial class AiAccountsController(AiAccountService aiAccountService) : ControllerBase
{
    [HttpGet(Name = "ListAiAccounts")]
    [Authorize(Policy = IdentityConstants.HumanSessionPolicy)]
    [ProducesResponseType<IReadOnlyList<AiAccountResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        var accounts = await aiAccountService.ListAsync(actorAccountId, cancellationToken);
        return accounts is null ? Forbid() : Ok(accounts);
    }

    [HttpPost(Name = "CreateAiAccount")]
    [Authorize(Policy = IdentityConstants.AiAccountCreatePermission)]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<CreateAiAccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        CreateAiAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        _ = AiFixedRoleCodes.TryParse(request.FixedRole ?? string.Empty, out var fixedRole);
        var result = await aiAccountService.CreateAsync(new CreateAiAccountCommand
        {
            ActorAccountId = actorAccountId,
            Username = request.Username,
            FixedRole = fixedRole,
        }, cancellationToken);
        return result.Outcome switch
        {
            AiAccountOperationOutcome.Succeeded => Created(
                $"/api/v1/ai-accounts/{result.Account!.Id}",
                new CreateAiAccountResponse
                {
                    Account = result.Account,
                    InitialPassword = result.InitialPassword!,
                }),
            AiAccountOperationOutcome.Forbidden => Forbid(),
            _ => ConflictProblem(
                "username_conflict",
                "https://khaikang.dev/problems/ai-accounts/username-conflict",
                "The username is already in use."),
        };
    }

    [HttpPut("{aiAccountId:guid}/api-enabled", Name = "SetAiAccountApiEnabled")]
    [Authorize(Policy = IdentityConstants.HumanSessionPolicy)]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<AiAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetApiEnabledAsync(
        Guid aiAccountId,
        SetAiApiEnabledRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version < 1)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Version)] = ["Version must be greater than zero."],
            }));
        }

        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        var result = await aiAccountService.SetApiEnabledAsync(new SetAiApiEnabledCommand
        {
            ActorAccountId = actorAccountId,
            AiAccountId = aiAccountId,
            Enabled = request.Enabled,
            Version = request.Version,
        }, cancellationToken);
        return MapAccountResult(result);
    }

    [HttpPost("{aiAccountId:guid}/token", Name = "CreateOrRecreateAiAccountToken")]
    [Authorize(Policy = IdentityConstants.HumanSessionPolicy)]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<AiTokenPlaintextResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IssueTokenAsync(
        Guid aiAccountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        var result = await aiAccountService.IssueTokenAsync(new AiTokenCommand
        {
            ActorAccountId = actorAccountId,
            AiAccountId = aiAccountId,
        }, cancellationToken);
        return result.Outcome == AiTokenOperationOutcome.Succeeded
            ? Created($"/api/v1/ai-accounts/{aiAccountId}/token", result.Token)
            : MapTokenFailure(result.Outcome);
    }

    [HttpGet("{aiAccountId:guid}/token", Name = "ViewAiAccountToken")]
    [Authorize(Policy = IdentityConstants.HumanSessionPolicy)]
    [ProducesResponseType<AiTokenPlaintextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewTokenAsync(
        Guid aiAccountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        var result = await aiAccountService.ViewTokenAsync(new AiTokenCommand
        {
            ActorAccountId = actorAccountId,
            AiAccountId = aiAccountId,
        }, cancellationToken);
        return result.Outcome == AiTokenOperationOutcome.Succeeded
            ? Ok(result.Token)
            : MapTokenFailure(result.Outcome);
    }

    [HttpDelete("{aiAccountId:guid}/token", Name = "RevokeAiAccountToken")]
    [Authorize(Policy = IdentityConstants.HumanSessionPolicy)]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeTokenAsync(
        Guid aiAccountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        var result = await aiAccountService.RevokeTokenAsync(new AiTokenCommand
        {
            ActorAccountId = actorAccountId,
            AiAccountId = aiAccountId,
        }, cancellationToken);
        return result.Outcome == AiTokenOperationOutcome.Succeeded
            ? NoContent()
            : MapTokenFailure(result.Outcome);
    }

    [HttpPut("{aiAccountId:guid}/owner", Name = "TransferAiAccountOwner")]
    [Authorize(Policy = IdentityConstants.HumanSessionPolicy)]
    [RequireAntiforgeryToken(true)]
    [ProducesResponseType<AiAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransferOwnerAsync(
        Guid aiAccountId,
        TransferAiAccountOwnerRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version < 1)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Version)] = ["Version must be greater than zero."],
            }));
        }

        if (!TryGetActorAccountId(out var actorAccountId))
        {
            return Unauthorized();
        }

        var result = await aiAccountService.TransferOwnerAsync(new TransferAiAccountOwnerCommand
        {
            ActorAccountId = actorAccountId,
            AiAccountId = aiAccountId,
            OwnerAccountId = request.OwnerAccountId,
            Version = request.Version,
        }, cancellationToken);
        return MapAccountResult(result);
    }

    private IActionResult MapAccountResult(AiAccountOperationResult result) => result.Outcome switch
    {
        AiAccountOperationOutcome.Succeeded => Ok(result.Account),
        AiAccountOperationOutcome.NotFound => NotFound(),
        AiAccountOperationOutcome.Forbidden => Forbid(),
        AiAccountOperationOutcome.OwnerNotActiveHuman => ConflictProblem(
            "owner_not_active_human",
            "https://khaikang.dev/problems/ai-accounts/owner-not-active-human",
            "The new owner must be an active human account."),
        _ => ConflictProblem(
            "ai_account_version_conflict",
            "https://khaikang.dev/problems/ai-accounts/version-conflict",
            "The AI account was updated by another user."),
    };

    private IActionResult MapTokenFailure(AiTokenOperationOutcome outcome) => outcome switch
    {
        AiTokenOperationOutcome.Forbidden => Forbid(),
        AiTokenOperationOutcome.AiApiDisabled => ConflictProblem(
            "ai_api_disabled",
            "https://khaikang.dev/problems/ai-accounts/api-disabled",
            "The AI API must be enabled before creating a token."),
        _ => NotFound(),
    };

    private IActionResult ConflictProblem(string code, string type, string detail)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            type: type,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    private bool TryGetActorAccountId(out Guid accountId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
    }

    private static IActionResult? ValidateCreateRequest(CreateAiAccountRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var username = request.Username?.Trim() ?? string.Empty;
        if (username.Length is < 4 or > 100 || !AiUsernameRegex().IsMatch(username))
        {
            errors[nameof(request.Username)] =
                ["AI username must start with 'ai-' and contain only letters, numbers, '.', '_' or '-'."];
        }

        if (!AiFixedRoleCodes.TryParse(request.FixedRole ?? string.Empty, out _))
        {
            errors[nameof(request.FixedRole)] =
                ["FixedRole must be pm_ai, development_ai, qa_ai, reviewer_ai or devops_ai."];
        }

        return errors.Count == 0
            ? null
            : new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }

    [GeneratedRegex("^ai-[A-Za-z0-9][A-Za-z0-9._-]{0,96}$", RegexOptions.CultureInvariant)]
    private static partial Regex AiUsernameRegex();
}
