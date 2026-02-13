using ManaFox.Core.Errors;
using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow;

public static class RitualExtensions
{
    /// <summary>
    /// Transform the flowing value, leaving tears untouched (functor)
    /// </summary>
    public static Ritual<TMapped> Map<T, TMapped>(
        this Ritual<T> ritual,
        Func<T, TMapped> map)
    {
        return ritual.IsFlowing
            ? Ritual<TMapped>.Flow(map(ritual.GetValue()!))
            : Ritual<TMapped>.Tear(ritual.GetTear()!);
    }

    /// <summary>
    /// Chain operations that can fail (monad bind)
    /// </summary>
    public static Ritual<TMapped> Bind<T, TMapped>(
        this Ritual<T> ritual,
        Func<T, Ritual<TMapped>> bind)
    {
        return ritual.IsFlowing
            ? bind(ritual.GetValue()!)
            : Ritual<TMapped>.Tear(ritual.GetTear()!);
    }

    /// <summary>
    /// Chain async operations that can fail
    /// </summary>
    public static async Task<Ritual<TMapped>> BindAsync<T, TMapped>(
        this Ritual<T> ritual,
        Func<T, Task<Ritual<TMapped>>> bind)
    {
        return ritual.IsFlowing
            ? await bind(ritual.GetValue()!)
            : Ritual<TMapped>.Tear(ritual.GetTear()!);
    }

    /// <summary>
    /// Chain async operations that can fail (async ritual input)
    /// </summary>
    public static async Task<Ritual<TMapped>> BindAsync<T, TMapped>(
        this Task<Ritual<T>> ritualTask,
        Func<T, Ritual<TMapped>> bind)
    {
        var ritual = await ritualTask;
        return ritual.IsFlowing
            ? bind(ritual.GetValue()!)
            : Ritual<TMapped>.Tear(ritual.GetTear()!);
    }

    /// <summary>
    /// Chain async operations that can fail (both async)
    /// </summary>
    public static async Task<Ritual<TMapped>> BindAsync<T, TMapped>(
        this Task<Ritual<T>> ritualTask,
        Func<T, Task<Ritual<TMapped>>> bind)
    {
        var ritual = await ritualTask;
        return ritual.IsFlowing
            ? await bind(ritual.GetValue()!)
            : Ritual<TMapped>.Tear(ritual.GetTear()!);
    }

    // ───────────────────────────────────────────────────────────
    //  Scrying (side effects)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Peek into the ritual for side effects without changing the railway track
    /// </summary>
    public static Ritual<T> Scry<T>(
        this Ritual<T> ritual,
        Action<T> action)
    {
        if (ritual.IsFlowing)
            action(ritual.GetValue()!);
        return ritual;
    }

    /// <summary>
    /// Peek into the ritual asynchronously for side effects
    /// </summary>
    public static async Task<Ritual<T>> ScryAsync<T>(
        this Ritual<T> ritual,
        Func<T, Task> action)
    {
        if (ritual.IsFlowing)
            await action(ritual.GetValue()!);
        return ritual;
    }

    /// <summary>
    /// Peek into the ritual asynchronously for side effects (async ritual input)
    /// </summary>
    public static async Task<Ritual<T>> ScryAsync<T>(
        this Task<Ritual<T>> ritualTask,
        Func<T, Task> action)
    {
        var ritual = await ritualTask;
        if (ritual.IsFlowing)
            await action(ritual.GetValue()!);
        return ritual;
    }

    /// <summary>
    /// Peek into failures for side effects
    /// </summary>
    public static Ritual<T> ScryTear<T>(
        this Ritual<T> ritual,
        Action<Tear> action)
    {
        if (ritual.IsTorn)
            action(ritual.GetTear()!);
        return ritual;
    }

    /// <summary>
    /// Peek into failures asynchronously for side effects
    /// </summary>
    public static async Task<Ritual<T>> ScryTearAsync<T>(
        this Ritual<T> ritual,
        Func<Tear, Task> action)
    {
        if (ritual.IsTorn)
            await action(ritual.GetTear()!);
        return ritual;
    }

    /// <summary>
    /// Peek into failures asynchronously for side effects (async ritual input)
    /// </summary>
    public static async Task<Ritual<T>> ScryTearAsync<T>(
        this Task<Ritual<T>> ritualTask,
        Func<Tear, Task> action)
    {
        var ritual = await ritualTask;
        if (ritual.IsTorn)
            await action(ritual.GetTear()!);
        return ritual;
    }

    // ───────────────────────────────────────────────────────────
    //  Recovery
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Recover from a tear by providing a fallback ritual
    /// </summary>
    public static Ritual<T> Recover<T>(
        this Ritual<T> ritual,
        Func<Tear, Ritual<T>> recovery)
    {
        return ritual.IsTorn
            ? recovery(ritual.GetTear()!)
            : ritual;
    }

    /// <summary>
    /// Recover from a tear asynchronously
    /// </summary>
    public static async Task<Ritual<T>> RecoverAsync<T>(
        this Ritual<T> ritual,
        Func<Tear, Task<Ritual<T>>> recovery)
    {
        return ritual.IsTorn
            ? await recovery(ritual.GetTear()!)
            : ritual;
    }

    /// <summary>
    /// Recover from a tear asynchronously (async ritual input)
    /// </summary>
    public static async Task<Ritual<T>> RecoverAsync<T>(
        this Task<Ritual<T>> ritualTask,
        Func<Tear, Task<Ritual<T>>> recovery)
    {
        var ritual = await ritualTask;
        return ritual.IsTorn
            ? await recovery(ritual.GetTear()!)
            : ritual;
    }

    // ───────────────────────────────────────────────────────────
    //  Unwrapping and fallbacks
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Get the value or provide a fallback
    /// </summary>
    public static T OrElse<T>(this Ritual<T> ritual, T fallback)
        => ritual.IsFlowing ? ritual.GetValue()! : fallback;

    /// <summary>
    /// Get the value or compute a fallback from the Tear
    /// </summary>
    public static T OrElse<T>(this Ritual<T> ritual, Func<Tear, T> fallback)
        => ritual.IsFlowing ? ritual.GetValue()! : fallback(ritual.GetTear()!);

    /// <summary>
    /// Get the value or compute a fallback asynchronously
    /// </summary>
    public static async Task<T> OrElseAsync<T>(this Ritual<T> ritual, Func<Tear, Task<T>> fallback)
        => ritual.IsFlowing ? ritual.GetValue()! : await fallback(ritual.GetTear()!);

    /// <summary>
    /// Get the value or compute a fallback asynchronously (async ritual input)
    /// </summary>
    public static async Task<T> OrElseAsync<T>(this Task<Ritual<T>> ritualTask, Func<Tear, Task<T>> fallback)
    {
        var ritual = await ritualTask;
        return ritual.IsFlowing ? ritual.GetValue()! : await fallback(ritual.GetTear()!);
    }

    /// <summary>
    /// Unwrap the value, throwing an exception if torn
    /// </summary>
    public static T Expect<T>(this Ritual<T> ritual, string message)
    {
        if (ritual.IsTorn)
            throw new TearException($"{message}: {ritual.GetTear()!.Message}", ritual.GetTear()!);
        return ritual.GetValue()!;
    }

    /// <summary>
    /// Unwrap the value, throwing an exception if torn
    /// </summary>
    public static T Unwrap<T>(this Ritual<T> ritual)
    {
        if (ritual.IsTorn)
            throw new TearException($"Called Unwrap on a torn Ritual: {ritual.GetTear()!.Message}", ritual.GetTear()!);
        return ritual.GetValue()!;
    }

    /// <summary>
    /// Unwrap the value asynchronously, throwing an exception if torn
    /// </summary>
    public static async Task<T> UnwrapAsync<T>(this Task<Ritual<T>> ritualTask)
    {
        var ritual = await ritualTask;
        if (ritual.IsTorn)
            throw new TearException($"Called Unwrap on a torn Ritual: {ritual.GetTear()!.Message}", ritual.GetTear()!);
        return ritual.GetValue()!;
    }

    // ───────────────────────────────────────────────────────────
    //  Conversions
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Convert a nullable reference to a Ritual
    /// </summary>
    public static Ritual<T> ToRitual<T>(this T? value, string message = "Value was null")
        where T : class
        => value != null
            ? Ritual<T>.Flow(value)
            : Ritual<T>.Tear(message);

    /// <summary>
    /// Convert a nullable value type to a Ritual
    /// </summary>
    public static Ritual<T> ToRitual<T>(this T? value, string message = "Value was null")
        where T : struct
        => value.HasValue
            ? Ritual<T>.Flow(value.Value)
            : Ritual<T>.Tear(message);

    /// <summary>
    /// Flatten nested Rituals
    /// </summary>
    public static Ritual<T> Flatten<T>(this Ritual<Ritual<T>> nested)
        => nested.Bind(inner => inner);

    /// <summary>
    /// Flatten nested Rituals asynchronously
    /// </summary>
    public static async Task<Ritual<T>> FlattenAsync<T>(this Task<Ritual<Ritual<T>>> nestedTask)
    {
        var nested = await nestedTask;
        return nested.Bind(inner => inner);
    }
}
