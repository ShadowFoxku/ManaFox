using ManaFox.Core.Errors;
using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow
{
    public static class RitualUnwrappingExtensions
    {
        /// <summary>
        /// Extract the flowing value, or return <paramref name="fallback"/> if the ritual is torn.
        /// </summary>
        public static T OrElse<T>(this Ritual<T> ritual, T fallback)
            => ritual.Match(onLeft: _ => fallback, onRight: v => v);

        /// <summary>
        /// Extract the flowing value, or compute a fallback from the tear if the ritual is torn.
        /// </summary>
        public static T OrElse<T>(this Ritual<T> ritual, Func<Tear, T> fallback)
            => ritual.Match(onLeft: fallback, onRight: v => v);

        /// <summary>
        /// Extract the flowing value, or asynchronously compute a fallback from the tear if the ritual is torn.
        /// </summary>
        public static async Task<T> OrElseAsync<T>(
            this Ritual<T> ritual, Func<Tear, Task<T>> fallback)
            => ritual.IsTorn ? await fallback(ritual.GetTear()!) : ritual.GetValue()!;

        /// <summary>
        /// Await a ritual task, then extract the flowing value or asynchronously compute a fallback from the tear.
        /// </summary>
        public static async Task<T> OrElseAsync<T>(
            this Task<Ritual<T>> ritualTask, Func<Tear, Task<T>> fallback)
            => await (await ritualTask).OrElseAsync(fallback);

        /// <summary>
        /// Extract the flowing value, or throw a <see cref="TearException"/> with a custom
        /// context message prepended to the tear's message if the ritual is torn.
        /// </summary>
        public static T Expect<T>(this Ritual<T> ritual, string message)
            => ritual.Match(
                onLeft: tear => throw new TearException($"{message}: {tear.Message}", tear),
                onRight: v => v);

        /// <summary>
        /// Extract the flowing value, or throw a <see cref="TearException"/> if the ritual is torn.
        /// Prefer <see cref="Expect{T}"/> when a meaningful context message is available.
        /// </summary>
        public static T Unwrap<T>(this Ritual<T> ritual)
            => ritual.Match(
                onLeft: tear => throw new TearException($"Called Unwrap on a torn Ritual: {tear.Message}", tear),
                onRight: v => v);

        /// <summary>
        /// Await a ritual task, then extract the flowing value or throw a <see cref="TearException"/>
        /// if the ritual is torn. Prefer <see cref="Expect{T}"/> when a meaningful context message is available.
        /// </summary>
        public static async Task<T> UnwrapAsync<T>(this Task<Ritual<T>> ritualTask)
            => (await ritualTask).Unwrap();
    }
}
