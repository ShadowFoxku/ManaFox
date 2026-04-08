using Microsoft.AspNetCore.Builder;

namespace ManaFox.Hosting.Middleware
{
    public static class DependencyInjection
    {
        public static IApplicationBuilder AddManaFoxMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorHandling.ErrorHandling>();
            app.UseMiddleware<Conventions.ApplyFromBodyConvention>();
            return app;
        }
    }
}
