using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using KhaiKang.Modules.CommentManagement.Infrastructure;
using KhaiKang.Modules.Identity.Contracts;
using KhaiKang.Modules.Identity.Infrastructure;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using KhaiKang.Modules.TestManagement.Contracts;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit.Sdk;

namespace KhaiKang.Api.IntegrationTests;

public sealed class TraceabilityPostgreSqlBehaviorTests
{
    private const string ConnectionVariable = "KHAIKANG_TRACEABILITY_POSTGRES_TEST_CONNECTION";

    [Fact]
    [Trait("Database", "PostgreSQL")]
    public async Task TraceabilityEndpoints_ReplayTerminalStatesAndRecoverSameFingerprintAfterTransientFailure()
    {
        var connectionString = RequireDisposableConnectionString();
        await RecreateDatabaseAsync(connectionString);
        using var factory = new TraceabilityPostgreSqlFactory(
            connectionString,
            new ThrowOnFirstIssueRelationInsertInterceptor(),
            new ThrowOnFirstCaseSourceInsertInterceptor());
        using var api = await PostgreSqlApiContext.CreateAsync(factory);

        var project = await CreateProjectAsync(api, "PGTRACE");
        var story = await CreateIssueAsync(api, project.Id, "Checkout story", "story");
        var task = await CreateIssueAsync(api, project.Id, "Test checkout", "task");
        var alternateTask = await CreateIssueAsync(api, project.Id, "Test refund", "task");
        var bug = await CreateIssueAsync(api, project.Id, "Checkout fails", "bug");
        var relationPath = $"/api/v1/projects/{project.Id}/issues/{story.Id}/relations";
        var relationRequest = new CreateIssueRelationRequest("parent_of", task.Id, "forward");

        var transient = await api.PostIdempotentAsync(relationPath, relationRequest, "pg-relation-transient");
        Assert.Equal(HttpStatusCode.InternalServerError, transient.StatusCode);
        var transientSubstitution = await api.PostIdempotentAsync(
            relationPath,
            new CreateIssueRelationRequest("parent_of", alternateTask.Id, "forward"),
            "pg-relation-transient");
        Assert.Equal(HttpStatusCode.Conflict, transientSubstitution.StatusCode);
        var recovered = await api.PostIdempotentAsync(relationPath, relationRequest, "pg-relation-transient");
        var replay = await api.PostIdempotentAsync(relationPath, relationRequest, "pg-relation-transient");
        Assert.Equal(HttpStatusCode.Created, recovered.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        var recoveredRelation = await recovered.Content.ReadFromJsonAsync<IssueRelationResponse>();
        var replayedRelation = await replay.Content.ReadFromJsonAsync<IssueRelationResponse>();
        Assert.Equal(recoveredRelation?.Id, replayedRelation?.Id);

        var rejectedRequest = new CreateIssueRelationRequest("parent_of", bug.Id, "forward");
        var rejected = await api.PostIdempotentAsync(relationPath, rejectedRequest, "pg-relation-rejected");
        var rejectedReplay = await api.PostIdempotentAsync(relationPath, rejectedRequest, "pg-relation-rejected");
        var rejectedSubstitution = await api.PostIdempotentAsync(
            relationPath,
            new CreateIssueRelationRequest("related", bug.Id, "forward"),
            "pg-relation-rejected");
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedReplay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejectedSubstitution.StatusCode);

        var concurrentStory = await CreateIssueAsync(api, project.Id, "Refund story", "story");
        var concurrentPath = $"/api/v1/projects/{project.Id}/issues/{concurrentStory.Id}/relations";
        var concurrentRequest = new CreateIssueRelationRequest("parent_of", alternateTask.Id, "forward");
        var concurrent = await Task.WhenAll(
            api.PostIdempotentAsync(concurrentPath, concurrentRequest, "pg-relation-concurrent"),
            api.PostIdempotentAsync(concurrentPath, concurrentRequest, "pg-relation-concurrent"));
        Assert.All(concurrent, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var concurrentBodies = await Task.WhenAll(
            concurrent[0].Content.ReadFromJsonAsync<IssueRelationResponse>(),
            concurrent[1].Content.ReadFromJsonAsync<IssueRelationResponse>());
        Assert.Equal(concurrentBodies[0]?.Id, concurrentBodies[1]?.Id);

        var workspace = await CreateWorkspaceAsync(api, "PGT");
        await LinkProjectAsync(api, workspace.Id, project.Id);
        var testCase = await CreateCaseAsync(api, workspace.Id);
        var casePath = $"/api/v1/test-workspaces/{workspace.Id}/cases/{testCase.Id}/source-test-issue";

        var caseTransient = await api.PostIdempotentAsync(
            casePath,
            new LinkTestCaseSourceRequest(task.Id),
            "pg-case-transient");
        Assert.Equal(HttpStatusCode.InternalServerError, caseTransient.StatusCode);
        var caseSubstitution = await api.PostIdempotentAsync(
            casePath,
            new LinkTestCaseSourceRequest(alternateTask.Id),
            "pg-case-transient");
        Assert.Equal(HttpStatusCode.Conflict, caseSubstitution.StatusCode);
        var caseRecovered = await api.PostIdempotentAsync(
            casePath,
            new LinkTestCaseSourceRequest(task.Id),
            "pg-case-transient");
        var caseReplay = await api.PostIdempotentAsync(
            casePath,
            new LinkTestCaseSourceRequest(task.Id),
            "pg-case-transient");
        Assert.Equal(HttpStatusCode.Created, caseRecovered.StatusCode);
        Assert.Equal(HttpStatusCode.Created, caseReplay.StatusCode);
        var recoveredSource = await caseRecovered.Content.ReadFromJsonAsync<TestCaseSourceResponse>();
        var replayedSource = await caseReplay.Content.ReadFromJsonAsync<TestCaseSourceResponse>();
        Assert.Equal(recoveredSource?.Id, replayedSource?.Id);

        var rejectedCase = await CreateCaseAsync(api, workspace.Id, "Story source is invalid");
        var rejectedCasePath = $"/api/v1/test-workspaces/{workspace.Id}/cases/{rejectedCase.Id}/source-test-issue";
        var invalidCaseSource = new LinkTestCaseSourceRequest(story.Id);
        var caseRejected = await api.PostIdempotentAsync(rejectedCasePath, invalidCaseSource, "pg-case-rejected");
        var caseRejectedReplay = await api.PostIdempotentAsync(rejectedCasePath, invalidCaseSource, "pg-case-rejected");
        var caseRejectedSubstitution = await api.PostIdempotentAsync(
            rejectedCasePath,
            new LinkTestCaseSourceRequest(alternateTask.Id),
            "pg-case-rejected");
        Assert.Equal(HttpStatusCode.BadRequest, caseRejected.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, caseRejectedReplay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, caseRejectedSubstitution.StatusCode);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(PostgreSqlApiContext api, string code)
    {
        var response = await api.PostJsonAsync(
            "/api/v1/projects",
            new CreateProjectRequest(code, $"{code} Project") { Description = null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ProjectResponse>(await response.Content.ReadFromJsonAsync<ProjectResponse>());
    }

    private static async Task<IssueResponse> CreateIssueAsync(
        PostgreSqlApiContext api,
        Guid projectId,
        string title,
        string typeCode)
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/projects/{projectId}/issues",
            new CreateIssueRequest(title, typeCode));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<IssueResponse>(await response.Content.ReadFromJsonAsync<IssueResponse>());
    }

    private static async Task<TestWorkspaceResponse> CreateWorkspaceAsync(PostgreSqlApiContext api, string prefix)
    {
        var response = await api.PostJsonAsync(
            "/api/v1/test-workspaces",
            new CreateTestWorkspaceRequest($"{prefix} Workspace") { Prefix = prefix, Description = null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<TestWorkspaceResponse>(await response.Content.ReadFromJsonAsync<TestWorkspaceResponse>());
    }

    private static async Task LinkProjectAsync(PostgreSqlApiContext api, Guid workspaceId, Guid projectId)
    {
        var response = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/projects",
            new LinkTestWorkspaceProjectRequest(projectId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<TestCaseResponse> CreateCaseAsync(
        PostgreSqlApiContext api,
        Guid workspaceId,
        string title = "Checkout succeeds")
    {
        var suiteResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/suites",
            new CreateTestSuiteRequest
            {
                ParentId = null,
                Name = $"Suite {Guid.NewGuid():N}",
                Description = null,
                SortOrder = 1,
            });
        Assert.Equal(HttpStatusCode.Created, suiteResponse.StatusCode);
        var suite = Assert.IsType<TestSuiteResponse>(
            await suiteResponse.Content.ReadFromJsonAsync<TestSuiteResponse>());
        var caseResponse = await api.PostJsonAsync(
            $"/api/v1/test-workspaces/{workspaceId}/cases",
            new CreateTestCaseRequest(
                suite.Id,
                title,
                [new CreateTestCaseStepRequest("Perform the operation.", "The result is visible.")])
            {
                SortOrder = 1,
            });
        Assert.Equal(HttpStatusCode.Created, caseResponse.StatusCode);
        return Assert.IsType<TestCaseResponse>(await caseResponse.Content.ReadFromJsonAsync<TestCaseResponse>());
    }

    private static async Task RecreateDatabaseAsync(string connectionString)
    {
        await using var identity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
        await identity.Database.EnsureDeletedAsync();
        await identity.Database.MigrateAsync();
        await using var comment = new CommentManagementDbContext(
            new DbContextOptionsBuilder<CommentManagementDbContext>().UseNpgsql(connectionString).Options);
        await comment.Database.MigrateAsync();
        await using var project = new ProjectManagementDbContext(
            new DbContextOptionsBuilder<ProjectManagementDbContext>().UseNpgsql(connectionString).Options);
        await project.Database.MigrateAsync();
        await using var test = new TestManagementDbContext(
            new DbContextOptionsBuilder<TestManagementDbContext>().UseNpgsql(connectionString).Options);
        await test.Database.MigrateAsync();
    }

    private static string RequireDisposableConnectionString()
    {
        var value = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(value))
            throw SkipException.ForSkip($"Set {ConnectionVariable} to an explicitly disposable PostgreSQL database.");
        var builder = new NpgsqlConnectionStringBuilder(value);
        if (builder.Database is null ||
            !builder.Database.StartsWith("khaikang_traceability_", StringComparison.Ordinal))
            throw new InvalidOperationException("Traceability PostgreSQL tests require a database prefixed khaikang_traceability_.");
        return value;
    }

    private sealed class PostgreSqlApiContext : IDisposable
    {
        private PostgreSqlApiContext(TraceabilityPostgreSqlFactory factory, HttpClient client)
        {
            Factory = factory;
            Client = client;
        }

        private TraceabilityPostgreSqlFactory Factory { get; }
        private HttpClient Client { get; }

        public static async Task<PostgreSqlApiContext> CreateAsync(TraceabilityPostgreSqlFactory factory)
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
            var context = new PostgreSqlApiContext(factory, client);
            try
            {
                var initialize = await context.PostWithoutIdempotencyAsync("/api/v1/setup/initialize", null);
                initialize.EnsureSuccessStatusCode();
                var credentials = await initialize.Content.ReadFromJsonAsync<InitializeAdminResponse>();
                Assert.NotNull(credentials);
                var login = await context.PostWithoutIdempotencyAsync(
                    "/api/v1/auth/login",
                    JsonContent.Create(new LoginRequest
                    {
                        Username = "admin",
                        Password = credentials.InitialPassword,
                        RememberMe = false,
                    }));
                login.EnsureSuccessStatusCode();
                return context;
            }
            catch
            {
                context.Dispose();
                throw;
            }
        }

        public Task<HttpResponseMessage> PostJsonAsync(string path, object content) =>
            PostIdempotentAsync(path, content, Guid.NewGuid().ToString("N"));

        public async Task<HttpResponseMessage> PostIdempotentAsync(string path, object content, string key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(content),
            };
            request.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync());
            request.Headers.Add("Idempotency-Key", key);
            return await Client.SendAsync(request);
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        private async Task<HttpResponseMessage> PostWithoutIdempotencyAsync(string path, HttpContent? content)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
            request.Headers.Add("X-XSRF-TOKEN", await GetCsrfTokenAsync());
            return await Client.SendAsync(request);
        }

        private async Task<string> GetCsrfTokenAsync()
        {
            var response = await Client.GetFromJsonAsync<CsrfTokenResponse>("/api/v1/auth/csrf-token");
            Assert.NotNull(response);
            return response.Token;
        }
    }

    private sealed class TraceabilityPostgreSqlFactory(
        string connectionString,
        DbCommandInterceptor issueRelationInterceptor,
        DbCommandInterceptor caseSourceInterceptor) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:KhaiKang", connectionString);
            builder.UseSetting("Attachments:LocalRoot", Path.Combine(
                Path.GetTempPath(),
                "khaikang-traceability-postgres-tests",
                Guid.NewGuid().ToString("N")));
            builder.ConfigureServices(services =>
            {
                ReplaceDbContext<ProjectManagementDbContext>(services, options =>
                    options.UseNpgsql(connectionString).AddInterceptors(issueRelationInterceptor));
                ReplaceDbContext<TestManagementDbContext>(services, options =>
                    options.UseNpgsql(connectionString).AddInterceptors(caseSourceInterceptor));
            });
        }

        private static void ReplaceDbContext<TContext>(
            IServiceCollection services,
            Action<DbContextOptionsBuilder> configure)
            where TContext : DbContext
        {
            services.RemoveAll<DbContextOptions<TContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TContext>>();
            services.RemoveAll<TContext>();
            services.AddDbContext<TContext>(configure);
        }
    }

    private sealed class ThrowOnFirstIssueRelationInsertInterceptor : DbCommandInterceptor
    {
        private int _remainingFailures = 1;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO issue_relations", StringComparison.OrdinalIgnoreCase) &&
                Interlocked.Exchange(ref _remainingFailures, 0) == 1)
                throw new TimeoutException("Injected transient PostgreSQL issue relation timeout.");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class ThrowOnFirstCaseSourceInsertInterceptor : DbCommandInterceptor
    {
        private int _remainingFailures = 1;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO test_case_test_issue_sources", StringComparison.OrdinalIgnoreCase) &&
                Interlocked.Exchange(ref _remainingFailures, 0) == 1)
                throw new TimeoutException("Injected transient PostgreSQL case source timeout.");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
