using ManaFox.Core.Errors;

namespace ManaFox.Core.Flow;

/// <summary>
/// A Ritual is an Either where Left is Tear (failure) and Right is the success value.
/// Rituals can be chained together in railway-oriented style.
/// </summary>
public class Ritual<T> : Either<Tear, T>
{
    private readonly Either<Tear, T> _inner;

    protected Ritual(Either<Tear, T> inner) => _inner = inner;

    /// <summary>
    /// Create a successful ritual
    /// </summary>
    public static Ritual<T> Flow(T value)
        => new(new Right<Tear, T>(value));

    /// <summary>
    /// Create a failed ritual from a Tear
    /// </summary>
    public static Ritual<T> Tear(Tear tear)
        => new(new Left<Tear, T>(tear));

    /// <summary>
    /// Create a failed ritual from an error message
    /// </summary>
    public static Ritual<T> Tear(string message)
        => new(new Left<Tear, T>(new Tear(message)));

    /// <summary>
    /// Create a failed ritual from an error message and code
    /// </summary>
    public static Ritual<T> Tear(string message, string code)
        => new(new Left<Tear, T>(new Tear(message, code)));

    // ───────────────────────────────────────────────────────────
    //  State inspection
    // ───────────────────────────────────────────────────────────

    public bool IsFlowing => _inner.IsRight;
    public bool IsTorn => _inner.IsLeft;

    public override bool IsLeft => IsTorn;
    public override bool IsRight => IsFlowing;

    public T? GetValue() => _inner.GetRight();
    public Tear? GetTear() => _inner.GetLeft();

    public override Tear? GetLeft() => GetTear();
    public override T? GetRight() => GetValue();

    // ───────────────────────────────────────────────────────────
    //  Either implementation (delegation)
    // ───────────────────────────────────────────────────────────

    public override Either<TLeftMapped, TRightMapped> Bimap<TLeftMapped, TRightMapped>(
        Func<Tear, TLeftMapped> leftMap,
        Func<T, TRightMapped> rightMap)
        => _inner.Bimap(leftMap, rightMap);

    public override TResult Match<TResult>(
        Func<Tear, TResult> onLeft,
        Func<T, TResult> onRight)
        => _inner.Match(onLeft, onRight);

    // ───────────────────────────────────────────────────────────
    //  Static helpers for exception handling
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Execute an operation that might throw, capturing exceptions as Tears
    /// </summary>
    public static Ritual<T> Try(Func<T> operation)
    {
        try
        {
            return Flow(operation());
        }
        catch (Exception ex)
        {
            return Ritual<T>.Tear(Errors.Tear.FromException(ex));
        }
    }

    /// <summary>
    /// Execute an async operation that might throw, capturing exceptions as Tears
    /// </summary>
    public static async Task<Ritual<T>> TryAsync(Func<Task<T>> operation)
    {
        try
        {
            return Flow(await operation());
        }
        catch (Exception ex)
        {
            return Ritual<T>.Tear(Errors.Tear.FromException(ex));
        }
    }
}

