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
    }
}
