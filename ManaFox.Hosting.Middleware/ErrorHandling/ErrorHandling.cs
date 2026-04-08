using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace ManaFox.Hosting.Middleware.ErrorHandling
{
    public class ErrorHandling(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context, ILogger<ErrorHandling> logger)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred while processing the request.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { HttpStatusCode = context.Response.StatusCode.ToString(), Error = "An unexpected error occurred." }));
            }
        }
    }
}
