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
    }
}
