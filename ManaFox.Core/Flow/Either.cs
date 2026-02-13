namespace ManaFox.Core.Flow;

/// <summary>
/// Represents a value that can be one of two types: Left or Right
/// </summary>
public abstract class Either<TLeft, TRight>
{
    public abstract bool IsLeft { get; }
    public abstract bool IsRight { get; }

    public abstract TLeft? GetLeft();
    public abstract TRight? GetRight();

    public abstract Either<TLeftMapped, TRightMapped> Bimap<TLeftMapped, TRightMapped>(
        Func<TLeft, TLeftMapped> leftMap,
        Func<TRight, TRightMapped> rightMap);

    public abstract TResult Match<TResult>(
        Func<TLeft, TResult> onLeft,
        Func<TRight, TResult> onRight);
}

/// <summary>
/// Left side of the Either
/// </summary>
internal sealed class Left<TLeft, TRight>(TLeft value) : Either<TLeft, TRight>
{
    private readonly TLeft _value = value;

    public override bool IsLeft => true;
    public override bool IsRight => false;

    public override TLeft GetLeft() => _value;
    public override TRight? GetRight() => default;

    public override Either<TLeftMapped, TRightMapped> Bimap<TLeftMapped, TRightMapped>(
        Func<TLeft, TLeftMapped> leftMap,
        Func<TRight, TRightMapped> rightMap)
        => new Left<TLeftMapped, TRightMapped>(leftMap(_value));

    public override TResult Match<TResult>(
        Func<TLeft, TResult> onLeft,
        Func<TRight, TResult> onRight)
        => onLeft(_value);
}

/// <summary>
/// Right side of the Either
/// </summary>
internal sealed class Right<TLeft, TRight>(TRight value) : Either<TLeft, TRight>
{
    private readonly TRight _value = value;

    public override bool IsLeft => false;
    public override bool IsRight => true;

    public override TLeft? GetLeft() => default;
    public override TRight GetRight() => _value;

    public override Either<TLeftMapped, TRightMapped> Bimap<TLeftMapped, TRightMapped>(
        Func<TLeft, TLeftMapped> leftMap,
        Func<TRight, TRightMapped> rightMap)
        => new Right<TLeftMapped, TRightMapped>(rightMap(_value));

    public override TResult Match<TResult>(
        Func<TLeft, TResult> onLeft,
        Func<TRight, TResult> onRight)
        => onRight(_value);
}