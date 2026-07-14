using KhaiKang.CommonUtils.Web.DependencyInjection;
using KhaiKang.Contracts.System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKhaiKangCommonWeb();

var app = builder.Build();

app.UseKhaiKangProblemDetails();

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
.WithName("GetSystemInfo");

app.Run();

public partial class Program;
