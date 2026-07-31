using KhaiKang.Modules.ProjectManagement.Application;
using KhaiKang.Modules.ProjectManagement.Controllers;
using KhaiKang.Modules.ProjectManagement.Endpoints;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Modules.ProjectManagement.DependencyInjection;

public static class ProjectManagementModuleExtensions
{
    public static IServiceCollection AddProjectManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KhaiKang")
            ?? throw new InvalidOperationException(
                "Connection string 'KhaiKang' is required.");

        services.AddDbContext<ProjectManagementDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<ProjectManagementService>();
        services.AddScoped<IssueService>();
        services.AddControllers()
            .AddApplicationPart(typeof(ProjectIssuesController).Assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapProjectManagementModule(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapProjectManagementEndpoints();
    }
}
