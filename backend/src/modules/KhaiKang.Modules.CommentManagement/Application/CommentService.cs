using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.CommentManagement.Domain;
using KhaiKang.Modules.CommentManagement.Infrastructure;
using KhaiKang.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KhaiKang.Modules.CommentManagement.Application;

public sealed class CommentService(
    CommentManagementDbContext dbContext,
    IEnumerable<ICommentResourceDirectory> resourceDirectories,
    IResourceActorDirectory actorDirectory,
    TimeProvider timeProvider)
{
    private const int MaximumContentLength = 10_000;
    private const string CreateOperationCode = "comment.create";
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CommentPageResponse?> ListAsync(
        ListCommentsQuery query,
        CancellationToken cancellationToken)
    {
        var actor = await actorDirectory.FindActiveAsync(
            query.ActorAccountId,
            cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var access = await GetAccessAsync(
            query.ResourceType,
            query.ResourceId,
            actor,
            cancellationToken);
        if (!access.CanRead)
        {
            return null;
        }

        const int pageSize = 20;
        var rows = await dbContext.Comments
            .AsNoTracking()
            .Where(comment =>
                comment.ResourceType == query.ResourceType &&
                comment.ResourceId == query.ResourceId &&
                !comment.IsDeleted)
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id)
            .Skip((query.Page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToArrayAsync(cancellationToken);

        return new CommentPageResponse
        {
            Items = rows.Take(pageSize).Select(ToResponse).ToArray(),
            Page = query.Page,
            HasNextPage = rows.Length > pageSize,
            CanCreate = access.CanCreate,
        };
    }

    public async Task<CreateCommentResult> CreateAsync(
        CreateCommentCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorDirectory.FindActiveAsync(command.ActorAccountId, cancellationToken);
        if (actor is null)
        {
            return CreateCommentResult.NotFound();
        }

        var access = await GetAccessAsync(
            command.ResourceType,
            command.ResourceId,
            actor,
            cancellationToken);
        if (!access.CanCreate)
        {
            return CreateCommentResult.NotFound();
        }

        var fingerprint = CreateFingerprint(command);
        await ReserveAsync(command, fingerprint, cancellationToken);
        return await ExecuteReservedAsync(
            command,
            ToCommentActorSnapshot(actor),
            fingerprint,
            cancellationToken);
    }

    private async Task ReserveAsync(
        CreateCommentCommand command,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var exists = await FindRecordQuery(command)
            .AsNoTracking()
            .AnyAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        var record = CommentIdempotencyRecord.Reserve(new CommentIdempotencyReservation
        {
            Id = Guid.NewGuid(),
            ActorAccountId = command.ActorAccountId,
            OperationCode = CreateOperationCode,
            IdempotencyKey = command.IdempotencyKey,
            RequestFingerprint = fingerprint,
            CreatedAt = timeProvider.GetUtcNow(),
        });

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.IdempotencyRecords.Add(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    private async Task<CreateCommentResult> ExecuteReservedAsync(
        CreateCommentCommand command,
        CommentActorSnapshot actor,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await FindAndLockRecordAsync(command, cancellationToken);

        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(record.RequestFingerprint),
            Convert.FromHexString(fingerprint)))
        {
            await transaction.RollbackAsync(cancellationToken);
            return CreateCommentResult.IdempotencyConflict();
        }

        if (record.State == CommentIdempotencyState.TerminalSuccess)
        {
            var replayResponse = JsonSerializer.Deserialize<CommentResponse>(
                record.ResponseSnapshot!,
                SnapshotJsonOptions)!;
            await transaction.CommitAsync(cancellationToken);
            return CreateCommentResult.Created(replayResponse);
        }

        if (record.State == CommentIdempotencyState.TerminalBusinessRejection)
        {
            var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
                record.RejectionSnapshot!,
                SnapshotJsonOptions)!;
            await transaction.CommitAsync(cancellationToken);
            return CreateCommentResult.Rejected(errors);
        }

        var validationErrors = ValidateContent(command.Content);
        if (validationErrors.Count > 0)
        {
            record.CompleteBusinessRejection(
                JsonSerializer.Serialize(validationErrors, SnapshotJsonOptions),
                timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CreateCommentResult.Rejected(validationErrors);
        }

        var comment = new Comment(new CommentCreation
        {
            Id = Guid.NewGuid(),
            ResourceType = command.ResourceType,
            ResourceId = command.ResourceId,
            Actor = actor,
            Content = command.Content.Trim(),
            CreatedAt = timeProvider.GetUtcNow(),
        });
        var response = ToResponse(comment);
        dbContext.Comments.Add(comment);
        record.CompleteSuccess(
            comment.Id,
            JsonSerializer.Serialize(response, SnapshotJsonOptions),
            timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreateCommentResult.Created(response);
    }

    private async Task<CommentIdempotencyRecord> FindAndLockRecordAsync(
        CreateCommentCommand command,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return await dbContext.IdempotencyRecords
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM comment_idempotency_records
                    WHERE actor_account_id = {command.ActorAccountId}
                      AND operation_code = {CreateOperationCode}
                      AND idempotency_key = {command.IdempotencyKey}
                    FOR UPDATE
                    """)
                .SingleAsync(cancellationToken);
        }

        return await FindRecordQuery(command).SingleAsync(cancellationToken);
    }

    private IQueryable<CommentIdempotencyRecord> FindRecordQuery(CreateCommentCommand command) =>
        dbContext.IdempotencyRecords.Where(record =>
            record.ActorAccountId == command.ActorAccountId &&
            record.OperationCode == CreateOperationCode &&
            record.IdempotencyKey == command.IdempotencyKey);

    private async Task<CommentResourceAccess> GetAccessAsync(
        CommentResourceType resourceType,
        Guid resourceId,
        ResourceActorSnapshot actor,
        CancellationToken cancellationToken)
    {
        var directory = resourceDirectories.SingleOrDefault(candidate => candidate.Supports(resourceType));
        return directory is null
            ? new CommentResourceAccess { CanRead = false, CanCreate = false }
            : await directory.GetAccessAsync(
                new CommentResourceAccessQuery
                {
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Actor = actor,
                },
                cancellationToken);
    }

    private static CommentActorSnapshot ToCommentActorSnapshot(ResourceActorSnapshot actor) => new()
    {
        AccountId = actor.AccountId,
        DisplayName = actor.AccountName,
        AccountType = actor.AccountType,
        FixedRole = actor.FixedRole,
        OwnerAccountId = actor.OwnerAccountId,
        OwnerDisplayName = actor.OwnerDisplayName,
    };

    private static Dictionary<string, string[]> ValidateContent(string? content)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(content) || content.Length > MaximumContentLength)
        {
            errors["content"] = ["Content is required and must be at most 10000 characters."];
            return errors;
        }

        var markdownError = CommentMarkdownPolicy.Validate(content);
        if (markdownError is not null)
        {
            errors["content"] = [markdownError];
        }

        return errors;
    }

    private static string CreateFingerprint(CreateCommentCommand command)
    {
        var canonicalRequest = JsonSerializer.Serialize(new
        {
            resourceType = ToResourceTypeCode(command.ResourceType),
            resourceId = command.ResourceId.ToString("D"),
            content = command.Content,
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
    }

    private static string ToResourceTypeCode(CommentResourceType resourceType) => resourceType switch
    {
        CommentResourceType.Issue => "issue",
        CommentResourceType.TestCase => "test_case",
        CommentResourceType.TestPlan => "test_plan",
        CommentResourceType.TestRun => "test_run",
        _ => throw new InvalidOperationException($"Unsupported comment resource type '{resourceType}'."),
    };

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };

    private static CommentResponse ToResponse(Comment comment) => new()
    {
        Id = comment.Id,
        AuthorAccountId = comment.AuthorAccountId,
        AuthorDisplayName = comment.AuthorDisplayName,
        AccountType = comment.AuthorAccountType,
        FixedRole = comment.AuthorFixedRole,
        OwnerAccountId = comment.AuthorOwnerAccountId,
        OwnerDisplayName = comment.AuthorOwnerDisplayName,
        Content = comment.Content,
        CreatedAt = comment.CreatedAt,
        Version = comment.Version,
    };
}
