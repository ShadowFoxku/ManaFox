namespace ManaFox.Core.Errors
{
    public sealed class Tear
    {
        public string Message { get; }
        public string? Code { get; }
        public Exception? InnerException { get; }

        public Tear(string message, string? code = null, Exception? innerException = null)
        {
            Message = message;
            Code = code;
            InnerException = innerException;
        }

        public static Tear FromException(Exception ex, string? code = null)
            => new(ex.Message, code, ex);

        public override string ToString()
            => Code != null ? $"[{Code}] {Message}" : Message;
    }

    public sealed class TearException : Exception
    {
        public Tear Tear { get; }

        public TearException(string message, Tear tear) : base(message)
        {
            Tear = tear;
        }

        public TearException(string message, Tear tear, Exception inner) : base(message, inner)
        {
            Tear = tear;
        }
    }
}
