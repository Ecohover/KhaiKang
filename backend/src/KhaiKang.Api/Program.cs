using System.Reflection;

using KhaiKang.Api.Configuration;
using KhaiKang.Api.Contracts;
using KhaiKang.CommonUtils.Web.DependencyInjection;
using KhaiKang.Modules.CommentManagement.DependencyInjection;
using KhaiKang.Modules.CommentManagement.Infrastructure;
using KhaiKang.Modules.Identity.DependencyInjection;
using KhaiKang.Modules.Identity.Infrastructure;
using KhaiKang.Modules.ProjectManagement.DependencyInjection;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using KhaiKang.Modules.TestManagement.DependencyInjection;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}

builder.Services.AddKhaiKangCommonWeb();
builder.Services.AddCommentManagementModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration, builder.Environment);
builder.Services.AddProjectManagementModule(builder.Configuration);
builder.Services.AddTestManagementModule(builder.Configuration);
builder.Services.AddOptions<DataProtectionKeyOptions>()
    .BindConfiguration(DataProtectionKeyOptions.SectionName)
    .Validate(
        options => builder.Environment.IsEnvironment("Testing") ||
            !string.IsNullOrWhiteSpace(options.KeysDirectory),
        "DataProtection:KeysDirectory is required outside Testing.")
    .ValidateOnStart();

var dataProtectionKeysDirectory = builder.Configuration[
    $"{DataProtectionKeyOptions.SectionName}:KeysDirectory"];
if (!builder.Environment.IsEnvironment("Testing") &&
    !string.IsNullOrWhiteSpace(dataProtectionKeysDirectory))
{
    Directory.CreateDirectory(dataProtectionKeysDirectory);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));
}

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.Replace(ServiceDescriptor.Singleton<IDataProtectionProvider>(
        new EphemeralDataProtectionProvider()));
}

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CommentManagementDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ProjectManagementDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<TestManagementDbContext>().Database.MigrateAsync();
}

app.UseKhaiKangProblemDetails();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Headers.Authorization.ToString()
        .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is not null)
        {
            var metadata = endpoint.Metadata
                .Where(item => item is not IAntiforgeryMetadata)
                .ToArray();
            context.SetEndpoint(new Endpoint(endpoint.RequestDelegate, new EndpointMetadataCollection(metadata), endpoint.DisplayName));
        }
    }

    await next(context);
});
app.UseAntiforgery();

app.MapControllers();
app.MapIdentityModule();
app.MapCommentManagementModule();
app.MapProjectManagementModule();
app.MapTestManagementModule();

app.MapGet("/openapi/v1.yaml", () => Results.File(
    Path.Combine(AppContext.BaseDirectory, "OpenApi", "khaikang.v1.yaml"),
    "application/yaml"))
    .WithName("GetOpenApiContract");

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
    .WithName("LiveHealth");

var system = app.MapGroup("/api/v1/system")
    .WithTags("System");

system.MapGet("/info", (IHostEnvironment environment) =>
{
    var version = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "0.0.0-dev";

    return Results.Ok(new SystemInfoResponse
    {
        ServiceName = "KhaiKang.Api",
        Version = version,
        Environment = environment.EnvironmentName,
        ServerTime = DateTimeOffset.UtcNow,
    });
})
.WithName("GetSystemInfo")
.Produces<SystemInfoResponse>();

app.Run();

public partial class Program;
