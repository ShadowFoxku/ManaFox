using ManaFox.Core.Errors;
using ManaFox.Core.Flow;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using ManaFox.Hosting.Middleware.ResponseWrapper;
using Microsoft.AspNetCore.Http;

namespace ManaFox.Hosting.Middleware.Controllers
{
    public class RitualControllerBase : ControllerBase
    {
        public HTTPTear APITear(string message, HttpStatusCode status)
        {
            return new HTTPTear(message, status);
        }

        [Obsolete("Use FromRitual<T> instead.")]
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
        
        protected IActionResult FromRitual<T>(Ritual<T> ritual, Func<string, string>? messageFormatter = null)
        {
            ArgumentNullException.ThrowIfNull(ritual);

            if (!ritual.IsTorn)
            {
                var sigil = SigilBuilder.Success()
                    .WithData(ritual.GetValue())
                    .Build();
                return Ok(sigil);
            }

            var tear = ritual.GetTear()!;
            if (tear.IsInternalTear)
            {
                if (tear.InnerException != null)
                    throw tear.InnerException;
                throw new Exception("An unhandled tear occurred. Message: " + tear.Message);
            }

            var message = messageFormatter?.Invoke(tear.Message) ?? tear.Message;

            var failure = SigilBuilder.Failure()
                .WithMessage(message)
                .WithError("ritual", tear.Message)
                .Build();

            if (tear is HTTPTear httpTear)
            {
                return httpTear.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => Unauthorized(),
                    HttpStatusCode.Forbidden => StatusCode(StatusCodes.Status403Forbidden, failure),
                    HttpStatusCode.NotFound => NotFound(failure),
                    _ => BadRequest(failure)
                };
            }

            return BadRequest(failure);
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
