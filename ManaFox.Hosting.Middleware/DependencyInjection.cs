using ManaFox.Hosting.Middleware.ResponseWrapper;
using Microsoft.AspNetCore.Builder;

namespace ManaFox.Hosting.Middleware
{
    public static class DependencyInjection
    {
        public static IApplicationBuilder AddManaFoxMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorHandling.ErrorHandling>();
            return app;
        }
        
        public static IApplicationBuilder AddSigilMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<WardMiddleware>();
            return app;
        }
    }
}
