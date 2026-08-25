using System.Security.Claims;
using KhaiKang.Modules.CommentManagement.Application;
using KhaiKang.Modules.CommentManagement.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KhaiKang.Modules.CommentManagement.Endpoints;

public static class CommentManagementEndpointExtensions
{
    public static IEndpointRouteBuilder MapCommentManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var comments = endpoints.MapGroup("/api/v1/comments").WithTags("Comments").RequireAuthorization();
        comments.MapGet("/{resourceType}/{resourceId:guid}", async (string resourceType, Guid resourceId, int? page, ClaimsPrincipal principal, CommentService service, CancellationToken token) =>
        {
            if (!TryGetAccountId(principal, out var accountId)) return Results.Unauthorized();
            if (!TryParseResourceType(resourceType, out var type) || page is < 1) return Results.NotFound();
            var response = await service.ListAsync(new ListCommentsQuery
            {
                ResourceType = type,
                ResourceId = resourceId,
                ActorAccountId = accountId,
                Page = page ?? 1,
            }, token);
            return response is null ? Results.NotFound() : Results.Ok(response);
        }).WithName("ListComments").Produces<CommentPageResponse>();

        comments.MapPost("/{resourceType}/{resourceId:guid}", async (string resourceType, Guid resourceId, CreateCommentRequest request, HttpRequest httpRequest, ClaimsPrincipal principal, IAntiforgery antiforgery, CommentService service, CancellationToken token) =>
        {
            if (!TryGetAccountId(principal, out var accountId)) return Results.Unauthorized();
            if (!TryParseResourceType(resourceType, out var type)) return Results.NotFound();
            var usesBearer = httpRequest.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            if (!usesBearer)
            {
                if (!await antiforgery.IsRequestValidAsync(httpRequest.HttpContext))
                {
                    return Results.BadRequest();
                }
            }

            if (!TryGetIdempotencyKey(httpRequest, out var idempotencyKey))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Idempotency-Key"] = ["A single non-empty Idempotency-Key header of at most 200 characters is required."],
                });
            }

            var result = await service.CreateAsync(new CreateCommentCommand
            {
                ResourceType = type,
                ResourceId = resourceId,
                ActorAccountId = accountId,
                IdempotencyKey = idempotencyKey,
                Content = request.Content,
            }, token);
            return result.Outcome switch
            {
                CreateCommentOutcome.Created => Results.Created(
                    $"/api/v1/comments/{resourceType}/{resourceId}/{result.Comment!.Id}",
                    result.Comment),
                CreateCommentOutcome.ValidationRejected => Results.ValidationProblem(
                    result.ValidationErrors!.ToDictionary(entry => entry.Key, entry => entry.Value)),
                CreateCommentOutcome.NotFound => Results.NotFound(),
                CreateCommentOutcome.IdempotencyConflict => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    type: "https://khaikang.dev/problems/idempotency-conflict",
                    title: "The idempotency key was already used for a different request.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "idempotency-conflict",
                    }),
                _ => throw new InvalidOperationException($"Unsupported create comment outcome '{result.Outcome}'."),
            };
        }).WithName("CreateComment")
            .Produces<CommentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static bool TryGetAccountId(ClaimsPrincipal principal, out Guid accountId) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out accountId);
    private static bool TryParseResourceType(string value, out CommentResourceType type)
    {
        type = value switch
        {
            "issue" => CommentResourceType.Issue,
            "test_case" => CommentResourceType.TestCase,
            "test_plan" => CommentResourceType.TestPlan,
            "test_run" => CommentResourceType.TestRun,
            _ => default,
        };
        return value is "issue" or "test_case" or "test_plan" or "test_run";
    }

    private static bool TryGetIdempotencyKey(HttpRequest request, out string idempotencyKey)
    {
        var values = request.Headers["Idempotency-Key"];
        idempotencyKey = values.Count == 1 ? values[0]?.Trim() ?? string.Empty : string.Empty;
        return idempotencyKey.Length is > 0 and <= 200;
    }
}
