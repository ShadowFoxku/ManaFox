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
        protected IActionResult FromRitual<T>(Ritual<T> ritual, Func<string, string>? messageFormatter = null)
        {
            ArgumentNullException.ThrowIfNull(ritual);

            if (ritual.IsTorn) return FromFailedRitual(ritual, messageFormatter);
            
            var sigil = SigilBuilder.Success().WithData(ritual.GetValue()).Build();
            return Ok(sigil); // i really would rather allow 201, 202, 204 in the future. need to fix this.
        }

        /// <summary>
        /// Same as the other, but lets the user handle their own success path, not auto-wrapped in a sigil.
        /// </summary>
        protected IActionResult FromRitual<T>(Ritual<T> ritual, Func<T, IActionResult> onSuccess,
            Func<string, string>? messageFormatter = null)
        {
            ArgumentNullException.ThrowIfNull(ritual);
            ArgumentNullException.ThrowIfNull(onSuccess);

            return !ritual.IsTorn ? onSuccess(ritual.GetValue()!) : FromFailedRitual(ritual, messageFormatter);
        }

        private IActionResult FromFailedRitual<T>(Ritual<T> ritual, Func<string, string>? messageFormatter)
        {
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
}
