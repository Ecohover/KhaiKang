using KhaiKang.Modules.Identity.Infrastructure;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Sdk;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TraceabilityPostgreSqlSchemaTests
{
    private const string ConnectionVariable = "KHAIKANG_TRACEABILITY_POSTGRES_TEST_CONNECTION";

    [Fact]
    [Trait("Database", "PostgreSQL")]
    public async Task Migrations_EnforceImmutableCardinalityRunMembershipAndIdempotencyState()
    {
        var connectionString = RequireDisposableConnectionString();
        await RecreateDatabaseAsync(connectionString);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var requiredObjects = new[]
        {
            "uq_test_case_test_issue_sources_case",
            "uq_bug_test_issue_sources_bug",
            "uq_test_run_bug_links_bug_issue",
            "uq_bug_test_run_item_sources_bug",
            "uq_traceability_idempotency_actor_key",
            "uq_issue_relation_idempotency_actor_key",
            "ck_traceability_idempotency_terminal",
            "ck_issue_relation_idempotency_terminal",
        };
        foreach (var name in requiredObjects)
        {
            await using var command = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = @name) OR EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = @name)",
                connection);
            command.Parameters.AddWithValue("name", name);
            Assert.True((bool)(await command.ExecuteScalarAsync())!, name);
        }

        await using (var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM pg_constraint WHERE conrelid = 'bug_test_run_item_sources'::regclass AND contype = 'f' AND pg_get_constraintdef(oid) LIKE '%(test_run_item_id, test_run_id)%'",
            connection))
        {
            Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
        }

        var actorId = Guid.NewGuid();
        var inserts = Enumerable.Range(0, 2).Select(async index =>
        {
            await using var concurrent = new NpgsqlConnection(connectionString);
            await concurrent.OpenAsync();
            await using var command = new NpgsqlCommand(
                "INSERT INTO traceability_idempotency_records (id, actor_account_id, idempotency_key, request_fingerprint, state, created_at) VALUES (@id, @actor, 'same-key', @fingerprint, 'incomplete_transient_failure', now())",
                concurrent);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("actor", actorId);
            command.Parameters.AddWithValue("fingerprint", new string((char)('A' + index), 64));
            return await Record.ExceptionAsync(() => command.ExecuteNonQueryAsync());
        });
        var results = await Task.WhenAll(inserts);
        Assert.Single(results, exception => exception is null);
        Assert.Single(results, ContainsUniqueViolation);

        await using var invalid = new NpgsqlCommand(
            "INSERT INTO traceability_idempotency_records (id, actor_account_id, idempotency_key, request_fingerprint, state, created_at) VALUES (@id, @actor, 'invalid-terminal', @fingerprint, 'terminal_success', now())",
            connection);
        invalid.Parameters.AddWithValue("id", Guid.NewGuid());
        invalid.Parameters.AddWithValue("actor", actorId);
        invalid.Parameters.AddWithValue("fingerprint", new string('C', 64));
        var checkFailure = await Record.ExceptionAsync(() => invalid.ExecuteNonQueryAsync());
        Assert.True(ContainsCheckViolation(checkFailure));
    }

    private static async Task RecreateDatabaseAsync(string connectionString)
    {
        var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
        await identity.Database.EnsureDeletedAsync();
        await identity.Database.MigrateAsync();
        await new ProjectManagementDbContext(new DbContextOptionsBuilder<ProjectManagementDbContext>().UseNpgsql(connectionString).Options).Database.MigrateAsync();
        await new TestManagementDbContext(new DbContextOptionsBuilder<TestManagementDbContext>().UseNpgsql(connectionString).Options).Database.MigrateAsync();
        await identity.DisposeAsync();
    }

    private static string RequireDisposableConnectionString()
    {
        var value = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(value)) throw SkipException.ForSkip($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
        var builder = new NpgsqlConnectionStringBuilder(value);
        if (builder.Database is null || !builder.Database.StartsWith("khaikang_traceability_", StringComparison.Ordinal))
            throw new InvalidOperationException("Traceability PostgreSQL tests require a database prefixed khaikang_traceability_.");
        return value;
    }

    private static bool ContainsUniqueViolation(Exception? exception) => ContainsSqlState(exception, PostgresErrorCodes.UniqueViolation);
    private static bool ContainsCheckViolation(Exception? exception) => ContainsSqlState(exception, PostgresErrorCodes.CheckViolation);
    private static bool ContainsSqlState(Exception? exception, string sqlState)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres && postgres.SqlState == sqlState) return true;
        return false;
    }
}
