using KhaiKang.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KhaiKang.Api.IntegrationTests;

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _sqliteServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .BuildServiceProvider();

    public IdentityApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:KhaiKang",
            "Host=localhost;Database=khaikang_testing;Username=testing;Password=testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddDbContext<IdentityDbContext>(options => options
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

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            _sqliteServices.Dispose();
        }
    }
}
