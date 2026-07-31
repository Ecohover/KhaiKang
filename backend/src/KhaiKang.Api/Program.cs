using KhaiKang.Api.Contracts;
using KhaiKang.CommonUtils.Web.DependencyInjection;
using KhaiKang.Modules.Identity.DependencyInjection;
using KhaiKang.Modules.ProjectManagement.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}

builder.Services.AddKhaiKangCommonWeb();
builder.Services.AddIdentityModule(builder.Configuration, builder.Environment);
builder.Services.AddProjectManagementModule(builder.Configuration);
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.Replace(ServiceDescriptor.Singleton<IDataProtectionProvider>(
        new EphemeralDataProtectionProvider()));
}

var app = builder.Build();

app.UseKhaiKangProblemDetails();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapIdentityModule();
app.MapProjectManagementModule();

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
    return Results.Ok(new SystemInfoResponse(
        "KhaiKang.Api",
        environment.EnvironmentName,
        DateTimeOffset.UtcNow));
})
.WithName("GetSystemInfo")
.Produces<SystemInfoResponse>();

app.Run();

public partial class Program;
