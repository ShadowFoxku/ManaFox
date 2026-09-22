namespace ManaFox.Hosting.Middleware.ResponseWrapper;

public class SigilBuilder
{
    protected bool IsSuccess { get; set; }
    protected string Message { get; set; } = "";
    protected Dictionary<string, string> Errors { get; set; } = [];
    protected Dictionary<string, string> Warnings { get; set; } = [];

    protected SigilBuilder(bool success)
    {
        IsSuccess = success;
    }

    public static SigilBuilder Failure()
    {
        return new SigilBuilder(false);
    }
    
    public static SigilBuilder Success()
    {
        return new SigilBuilder(true);
    }

    public virtual Sigil Build()
    {
        return new Sigil()
        {
            Message = Message,
            Success = IsSuccess,
            Errors = Errors,
            Warnings = Warnings,
        };
    }

    public SigilBuilder WithMessage(string message)
    {
        Message = message;
        return this;
    }

    public SigilBuilder<T> WithData<T>(T data) => new(IsSuccess, data)
    {
        Message = Message,
        Errors = Errors,
        Warnings = Warnings,
    };
    
    public SigilBuilder WithError(string error) => WithError("", error);

    public SigilBuilder WithError(string key, string message)
    {
        Errors.Add(key, message); 
        return this;
    }
    
    public SigilBuilder WithErrors(params (string Key, string Message)[] errors)
    {
        foreach (var (key, message) in errors) WithError(key, message);
        return this;
    }

    public SigilBuilder WithWarning(string warning) => WithWarning("", warning);

    public SigilBuilder WithWarning(string key, string message)
    {
        Warnings.Add(key, message); 
        return this;
    }
    
    public SigilBuilder WithWarnings(params (string Key, string Message)[] warnings)
    {
        foreach (var (key, message) in warnings) WithWarning(key, message);
        return this;
    }
}

public sealed class SigilBuilder<T> : SigilBuilder
{
    private readonly T _data;

    internal SigilBuilder(bool success, T data) : base(success) => _data = data;

    public override Sigil Build() => new Sigil<T>
    {
        Message = Message,
        Success = IsSuccess,
        Errors = Errors,
        Warnings = Warnings,
        Data = _data,
    };

    public new SigilBuilder<T> WithMessage(string message)
    {
        base.WithMessage(message);
        return this;
    }

    public new SigilBuilder<T> WithError(string error)
    {
        base.WithError(error);
        return this;
    }

    public new SigilBuilder<T> WithError(string key, string message)
    {
        base.WithError(key, message);
        return this;
    }

    public new SigilBuilder<T> WithErrors(params (string Key, string Message)[] errors)
    {
        base.WithErrors(errors);
        return this;
    }

    public new SigilBuilder<T> WithWarning(string warning)
    {
        base.WithWarning(warning);
        return this;
    }

    public new SigilBuilder<T> WithWarning(string key, string message)
    {
        base.WithWarning(key, message);
        return this;
    }

    public new SigilBuilder<T> WithWarnings(params (string Key, string Message)[] warnings)
    {
        base.WithWarnings(warnings);
        return this;
    }
}