using System.Data.Common;
using KhaiKang.Modules.CommentManagement.Application;
using KhaiKang.Modules.CommentManagement.Contracts;
using KhaiKang.Modules.CommentManagement.Domain;
using KhaiKang.Modules.CommentManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xunit.Sdk;

namespace KhaiKang.Api.IntegrationTests;

public sealed class CommentIdempotencyPostgreSqlTests
{
    private const string ConnectionVariable = "KHAIKANG_COMMENT_POSTGRES_TEST_CONNECTION";

    [Fact]
    [Trait("Database", "PostgreSQL")]
    public async Task CreateComment_UsesDurableReservationAtomicTerminalStatesAndConcurrentReplay()
    {
        var connectionString = RequireDisposableConnectionString();
        await RecreateDatabaseAsync(connectionString);
        var actorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var successCommand = CreateCommand(actorId, resourceId, "success-key", "Original payload.");
        var first = await ExecuteAsync(connectionString, successCommand);
        var replay = await ExecuteAsync(connectionString, successCommand);
        var successConflict = await ExecuteAsync(
            connectionString,
            CreateCommand(actorId, resourceId, "success-key", "Substituted payload."));

        Assert.Equal(CreateCommentOutcome.Created, first.Outcome);
        Assert.Equal(first.Comment!.Id, replay.Comment!.Id);
        Assert.Equal(CreateCommentOutcome.IdempotencyConflict, successConflict.Outcome);

        var rejectionCommand = CreateCommand(
            actorId,
            resourceId,
            "rejection-key",
            new string('x', 10_001));
        var rejection = await ExecuteAsync(connectionString, rejectionCommand);
        var rejectionReplay = await ExecuteAsync(connectionString, rejectionCommand);
        var rejectionConflict = await ExecuteAsync(
            connectionString,
            CreateCommand(actorId, resourceId, "rejection-key", "Valid replacement."));

        Assert.Equal(CreateCommentOutcome.ValidationRejected, rejection.Outcome);
        Assert.Equal(rejection.ValidationErrors, rejectionReplay.ValidationErrors);
        Assert.Equal(CreateCommentOutcome.IdempotencyConflict, rejectionConflict.Outcome);

        var transientCommand = CreateCommand(actorId, resourceId, "transient-key", "Recoverable payload.");
        var transientException = await Record.ExceptionAsync(() => ExecuteAsync(
            connectionString,
            transientCommand,
            new ThrowOnFirstCommentInsertInterceptor()));
        Assert.NotNull(transientException);
        Assert.True(ContainsException<TimeoutException>(transientException));

        await using (var inspection = CreateDbContext(connectionString))
        {
            var incomplete = await inspection.IdempotencyRecords.SingleAsync(record =>
                record.ActorAccountId == actorId &&
                record.OperationCode == "comment.create" &&
                record.IdempotencyKey == "transient-key");
            Assert.Equal(CommentIdempotencyState.IncompleteTransientFailure, incomplete.State);
            Assert.Empty(await inspection.Comments.Where(comment => comment.ResourceId == resourceId && comment.Content == "Recoverable payload.").ToArrayAsync());
        }

        var recovered = await ExecuteAsync(connectionString, transientCommand);
        Assert.Equal(CreateCommentOutcome.Created, recovered.Outcome);

        var concurrentCommand = CreateCommand(actorId, resourceId, "concurrent-key", "Concurrent payload.");
        var concurrentResults = await Task.WhenAll(
            ExecuteAsync(connectionString, concurrentCommand),
            ExecuteAsync(connectionString, concurrentCommand));

        Assert.All(concurrentResults, result => Assert.Equal(CreateCommentOutcome.Created, result.Outcome));
        Assert.Equal(concurrentResults[0].Comment!.Id, concurrentResults[1].Comment!.Id);

        await using var verification = CreateDbContext(connectionString);
        Assert.Equal(3, await verification.Comments.CountAsync(comment => comment.ResourceId == resourceId));
        Assert.Equal(4, await verification.IdempotencyRecords.CountAsync(record => record.ActorAccountId == actorId));
        Assert.All(
            await verification.IdempotencyRecords.Where(record => record.ActorAccountId == actorId).ToArrayAsync(),
            record => Assert.NotEmpty(record.RequestFingerprint));
    }

    private static async Task<CreateCommentResult> ExecuteAsync(
        string connectionString,
        CreateCommentCommand command,
        DbCommandInterceptor? interceptor = null)
    {
        await using var dbContext = CreateDbContext(connectionString, interceptor);
        var service = new CommentService(
            dbContext,
            [new AllowedCommentResourceDirectory()],
            new ActiveHumanActorDirectory(command.ActorAccountId),
            TimeProvider.System);
        return await service.CreateAsync(command, CancellationToken.None);
    }

    private static CommentManagementDbContext CreateDbContext(
        string connectionString,
        DbCommandInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<CommentManagementDbContext>()
            .UseNpgsql(connectionString);
        if (interceptor is not null)
        {
            options.AddInterceptors(interceptor);
        }

        return new CommentManagementDbContext(options.Options);
    }

    private static async Task RecreateDatabaseAsync(string connectionString)
    {
        await using var dbContext = CreateDbContext(connectionString);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    private static CreateCommentCommand CreateCommand(
        Guid actorAccountId,
        Guid resourceId,
        string idempotencyKey,
        string content) => new()
        {
            ResourceType = CommentResourceType.Issue,
            ResourceId = resourceId,
            ActorAccountId = actorAccountId,
            IdempotencyKey = idempotencyKey,
            Content = content,
        };

    private static string RequireDisposableConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw SkipException.ForSkip($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Database is null ||
            !builder.Database.StartsWith("khaikang_comment_idempotency_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The PostgreSQL test database name must start with 'khaikang_comment_idempotency_'; actual '{builder.Database}'.");
        }

        return connectionString;
    }

    private static bool ContainsException<TException>(Exception exception)
        where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class AllowedCommentResourceDirectory : ICommentResourceDirectory
    {
        public bool Supports(CommentResourceType resourceType) => resourceType == CommentResourceType.Issue;

        public Task<CommentResourceAccess> GetAccessAsync(
            CommentResourceType resourceType,
            Guid resourceId,
            Guid accountId,
            CancellationToken cancellationToken) => Task.FromResult(new CommentResourceAccess
            {
                CanRead = true,
                CanCreate = true,
            });
    }

    private sealed class ActiveHumanActorDirectory(Guid accountId) : ICommentActorDirectory
    {
        public Task<CommentActorSnapshot?> FindActiveAsync(
            Guid requestedAccountId,
            CancellationToken cancellationToken) => Task.FromResult<CommentActorSnapshot?>(
                requestedAccountId == accountId
                    ? new CommentActorSnapshot
                    {
                        AccountId = accountId,
                        DisplayName = "postgres-test-actor",
                        AccountType = "human",
                    }
                    : null);
    }

    private sealed class ThrowOnFirstCommentInsertInterceptor : DbCommandInterceptor
    {
        private int _remainingFailures = 1;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO comments", StringComparison.OrdinalIgnoreCase) &&
                Interlocked.Exchange(ref _remainingFailures, 0) == 1)
            {
                throw new TimeoutException("Injected transient PostgreSQL command timeout.");
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
