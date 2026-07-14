using Microsoft.AspNetCore.Builder;

namespace KhaiKang.CommonUtils.Web.DependencyInjection;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseKhaiKangProblemDetails(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }
}
