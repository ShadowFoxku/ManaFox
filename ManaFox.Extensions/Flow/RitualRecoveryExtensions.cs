using ManaFox.Core.Errors;
using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow
{
    public static class RitualRecoveryExtensions
    {    
        /// <summary>
        /// Recover from a torn ritual by supplying a fallback ritual derived from the tear.
        /// If the ritual is flowing, <paramref name="recovery"/> is never invoked.
        /// </summary>
        public static Ritual<T> Recover<T>(
            this Ritual<T> ritual,
            Func<Tear, Ritual<T>> recovery)
            => ritual.Match(onLeft: recovery, onRight: Ritual<T>.Flow);

        /// <summary>
        /// Recover from a torn ritual asynchronously by supplying a fallback ritual derived from the tear.
        /// If the ritual is flowing, <paramref name="recovery"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<T>> RecoverAsync<T>(
            this Ritual<T> ritual,
            Func<Tear, Task<Ritual<T>>> recovery)
            => ritual.IsTorn
                ? await recovery(ritual.GetTear()!)
                : ritual;

        /// <summary>
        /// Await a ritual task, then recover from a tear asynchronously by supplying a fallback ritual.
        /// If the ritual is flowing, <paramref name="recovery"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<T>> RecoverAsync<T>(
            this Task<Ritual<T>> ritualTask,
            Func<Tear, Task<Ritual<T>>> recovery)
            => await (await ritualTask).RecoverAsync(recovery);
    }
}
