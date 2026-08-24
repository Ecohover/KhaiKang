using KhaiKang.Modules.CommentManagement.Application;
using KhaiKang.Modules.CommentManagement.Endpoints;
using KhaiKang.Modules.CommentManagement.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KhaiKang.Modules.CommentManagement.DependencyInjection;

public static class CommentManagementModuleExtensions
{
    public static IServiceCollection AddCommentManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KhaiKang") ?? throw new InvalidOperationException("Connection string 'KhaiKang' is required.");
        services.AddDbContext<CommentManagementDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<CommentService>();
        return services;
    }

    public static IEndpointRouteBuilder MapCommentManagementModule(this IEndpointRouteBuilder endpoints) => endpoints.MapCommentManagementEndpoints();
}
