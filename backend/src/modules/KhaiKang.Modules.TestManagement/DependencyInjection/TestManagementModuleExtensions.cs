using KhaiKang.Modules.TestManagement.Application;
using KhaiKang.Modules.TestManagement.Controllers;
using KhaiKang.Modules.TestManagement.Endpoints;
using KhaiKang.Modules.TestManagement.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Modules.TestManagement.DependencyInjection;

public static class TestManagementModuleExtensions
{
    public static IServiceCollection AddTestManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KhaiKang")
            ?? throw new InvalidOperationException("Connection string 'KhaiKang' is required.");
        services.AddDbContext<TestManagementDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<TestManagementService>();
        services.AddScoped<TestCaseAttachmentService>();
        services.AddScoped<TestRunItemAttachmentService>();
        services.AddControllers()
            .AddApplicationPart(typeof(TestCaseAttachmentsController).Assembly);
        return services;
    }

    public static IEndpointRouteBuilder MapTestManagementModule(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapTestManagementEndpoints();
}
