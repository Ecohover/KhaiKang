using KhaiKang.CommonUtils.Storage;
using KhaiKang.Modules.ProjectManagement.Application;
using KhaiKang.Modules.ProjectManagement.Contracts;
using KhaiKang.Modules.ProjectManagement.Controllers;
using KhaiKang.Modules.ProjectManagement.Endpoints;
using KhaiKang.Modules.ProjectManagement.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Validate(options => string.Equals(options.Provider, "local", StringComparison.OrdinalIgnoreCase),
                "Only the local attachment provider is available in the MVP.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.LocalRoot),
                "Attachments:LocalRoot is required.")
            .Validate(options => options.MaxFileSizeBytes > 0,
                "Attachments:MaxFileSizeBytes must be greater than zero.")
            .ValidateOnStart();
        services.AddSingleton<IFileStorage>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            return new LocalFileStorage(options.LocalRoot);
        });
        services.AddScoped<ProjectManagementService>();
        services.AddScoped<IProjectDirectory, ProjectDirectory>();
        services.AddScoped<IIssueDirectory, IssueDirectory>();
        services.AddScoped<IIssueCommandService, IssueCommandService>();
        services.AddScoped<IssueService>();
        services.AddScoped<IssueRelationService>();
        services.AddScoped<IssueAttachmentService>();
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
