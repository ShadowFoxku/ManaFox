using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ManaFox.Hosting.Middleware.ResponseWrapper;

public class WardMiddleware(RequestDelegate next, IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next = next;
    private readonly IHostEnvironment _env = env;

    public async Task Invoke(HttpContext context, ILogger<WardMiddleware> logger)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request.");

            var detail = _env.IsDevelopment() ? ex.Message : "Contact support if this persists.";

            var sigil = SigilBuilder.Failure()
                .WithMessage("An unexpected error occurred.")
                .WithError("unhandled", detail)
                .Build();

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(sigil, sigil.GetType(), JsonOptions));
        }
    }
}