using System.Net;

namespace ManaFox.Core.Errors
{
    public class HTTPTear(string message, HttpStatusCode httpStatus) : Tear(message, $"HTTP {(int)httpStatus}", null)
    {
        public HttpStatusCode StatusCode { get; } = httpStatus;
    }
}
