using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ManaFox.Hosting.Middleware.Controllers
{
    public class RitualControllerBase : ControllerBase
    {
        public HTTPTear APITear(string message, HttpStatusCode status)
        {
            return new HTTPTear(message, status);
        }

        public bool IsRitualValid<T>(Ritual<T> ritual, Func<string, string> messageFormatter, out IActionResult result)
        {
            ArgumentNullException.ThrowIfNull(ritual);
            result = Accepted(ApiMessageResponse.Standard("Handler behaviour is inconsistent. Please contact support."));

            if (ritual.IsTorn)
            {
                var tear = ritual.GetTear()!;

                if (tear.IsInternalTear)
                {   // bad state, we should handle this before we reach the API validation layer
                    if (tear.InnerException != null)
                        throw tear.InnerException;
                    throw new Exception($"An unhandled tear occured. Message: {tear.Message}");
                }

                var message = messageFormatter?.Invoke(tear.Message) ?? tear.Message;
                var messageBody = ApiMessageResponse.Standard(message);

                result = tear is HTTPTear http
                    ? http.StatusCode switch
                    {
                        HttpStatusCode.NotFound => NotFound(messageBody),
                        HttpStatusCode.Unauthorized => Unauthorized(),
                        HttpStatusCode.Forbidden => Forbid(),
                        _ => BadRequest(messageBody),
                    }
                    : BadRequest(messageBody);

                return false;
            }

            return true;
        }
    }

    public class ApiMessageResponse(string message)
    {
        public static ApiMessageResponse Standard(string message)
        {
            return new ApiMessageResponse(message);
        }

        public string Message { get; } = message;
    }
}
