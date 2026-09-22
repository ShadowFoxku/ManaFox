namespace ManaFox.Hosting.Middleware.ResponseWrapper;

public record Sigil
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public Dictionary<string, string> Errors { get; init; }
    public Dictionary<string, string> Warnings { get; init; }
}

public record Sigil<T> : Sigil
{
    public T? Data  { get; init; }
}
