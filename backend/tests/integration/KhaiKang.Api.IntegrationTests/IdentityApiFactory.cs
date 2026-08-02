using KhaiKang.Modules.Identity.Domain;
using KhaiKang.Modules.Identity.Infrastructure;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=localhost;Database=khaikang_testing;Username=testing;Password=testing";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string? _previousConnectionString;
    private readonly ServiceProvider _sqliteServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .BuildServiceProvider();

    public IdentityApiFactory()
    {
        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__KhaiKang");
        Environment.SetEnvironmentVariable("ConnectionStrings__KhaiKang", TestConnectionString);
        _connection.Open();
    }

    public async Task<Guid> AddActiveAccountAsync(string username)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
        var now = DateTimeOffset.UtcNow;
        var account = new Account(Guid.NewGuid(), username, username.ToUpperInvariant(), now);
        account.SetInitialPassword(passwordHasher.HashPassword(account, "Temporary-Pass-123!"));
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddDbContext<IdentityDbContext>(options => options
                .UseSqlite(_connection)
                .UseInternalServiceProvider(_sqliteServices));
            services.RemoveAll<DbContextOptions<ProjectManagementDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ProjectManagementDbContext>>();
            services.RemoveAll<ProjectManagementDbContext>();
            services.AddDbContext<ProjectManagementDbContext>(options => options
                .UseSqlite(_connection)
                .UseInternalServiceProvider(_sqliteServices));
            services.RemoveAll<DbContextOptions<TestManagementDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TestManagementDbContext>>();
            services.RemoveAll<TestManagementDbContext>();
            services.AddDbContext<TestManagementDbContext>(options => options
                .UseSqlite(_connection)
                .UseInternalServiceProvider(_sqliteServices));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        dbContext.Database.EnsureCreated();
        var projectDbContext = scope.ServiceProvider.GetRequiredService<ProjectManagementDbContext>();
        projectDbContext.Database.GetService<IRelationalDatabaseCreator>().CreateTables();
        var testManagementDbContext = scope.ServiceProvider.GetRequiredService<TestManagementDbContext>();
        testManagementDbContext.Database.GetService<IRelationalDatabaseCreator>().CreateTables();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            _sqliteServices.Dispose();
            Environment.SetEnvironmentVariable("ConnectionStrings__KhaiKang", _previousConnectionString);
        }
    }
}
