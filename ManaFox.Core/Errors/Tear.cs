namespace ManaFox.Core.Errors
{
    public sealed class Tear(string message, string? code = null, Exception? innerException = null)
    {
        private Tear(string message, string? code = null, Exception? innerException = null, bool isInteralTear = true) : this(message, code, innerException)
        {
            IsInternalTear = isInteralTear;
        }

        public string Message { get; } = message;
        public string? Code { get; } = code;
        public Exception? InnerException { get; } = innerException;

        public bool IsInternalTear { get; } = false;

        public static Tear FromException(Exception ex, string? code = null)
            => new(ex.Message, code, ex);

        internal static Tear FromExceptionInternal(Exception ex, string? code = null)
            => new(ex.Message, code, ex, true);

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
