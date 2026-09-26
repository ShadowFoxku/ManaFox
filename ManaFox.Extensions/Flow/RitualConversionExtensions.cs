using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow
{
    public static class RitualConversionExtensions
    {
        /// <summary>
        /// Convert a nullable reference type to a ritual. Produces a flowing ritual if the value
        /// is non-null, or a torn ritual with <paramref name="message"/> if it is null.
        /// </summary>
        public static Ritual<T> ToRitual<T>(this T? value, string message = "Value was null")
            where T : class
            => value is not null ? Ritual<T>.Flow(value) : Ritual<T>.Tear(message);

        /// <summary>
        /// Convert a nullable value type to a ritual. Produces a flowing ritual if the value
        /// has a value, or a torn ritual with <paramref name="message"/> if it is null.
        /// </summary>
        public static Ritual<T> ToRitual<T>(this T? value, string message = "Value was null")
            where T : struct
            => value.HasValue ? Ritual<T>.Flow(value.Value) : Ritual<T>.Tear(message);

        /// <summary>
        /// Flatten a nested <see cref="Ritual{T}"/> into a single ritual. Equivalent to
        /// <c>Bind(inner => inner)</c>.
        /// </summary>
        public static Ritual<T> Flatten<T>(this Ritual<Ritual<T>> nested) => nested.Bind(inner => inner);

        /// <summary>
        /// Await a nested ritual task, then flatten it into a single ritual. Equivalent to
        /// awaiting and then calling <see cref="Flatten{T}"/>.
        /// </summary>
        public static async Task<Ritual<T>> FlattenAsync<T>(this Task<Ritual<Ritual<T>>> nestedTask) => (await nestedTask).Flatten();
        
        public static async Task<Ritual<(T1, T2)>> WeaveAsync<T1, T2>(this Task<Ritual<T1>> ritual1, Task<Ritual<T2>> ritual2)
        {
            await Task.WhenAll(ritual1, ritual2);

            var r1 = ritual1.Result;
            var r2 = ritual2.Result;

            if (r1.IsTorn) return Ritual<(T1, T2)>.Tear(r1.GetTear()!);
            if (r2.IsTorn) return Ritual<(T1, T2)>.Tear(r2.GetTear()!);

            return Ritual<(T1, T2)>.Flow((r1.GetValue()!, r2.GetValue()!));
        }
    }
}
