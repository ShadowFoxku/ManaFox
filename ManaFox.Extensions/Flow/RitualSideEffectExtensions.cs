using ManaFox.Core.Errors;
using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow
{
    public static class RitualSideEffectExtensions
    {    
        /// <summary>
        /// Peek at the flowing value for a side effect without altering the ritual.
        /// If the ritual is torn, <paramref name="action"/> is never invoked.
        /// </summary>
        public static Ritual<T> Scry<T>(this Ritual<T> ritual, Action<T> action)
        {
            if (ritual.IsFlowing) action(ritual.GetValue()!);
            return ritual;
        }

        /// <summary>
        /// Peek at the flowing value asynchronously for a side effect without altering the ritual.
        /// If the ritual is torn, <paramref name="action"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<T>> ScryAsync<T>(
            this Ritual<T> ritual, Func<T, Task> action)
        {
            if (ritual.IsFlowing) await action(ritual.GetValue()!);
            return ritual;
        }

        /// <summary>
        /// Await a ritual task, then peek at the flowing value asynchronously for a side effect
        /// without altering the ritual. If the ritual is torn, <paramref name="action"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<T>> ScryAsync<T>(
            this Task<Ritual<T>> ritualTask, Func<T, Task> action)
            => await (await ritualTask).ScryAsync(action);

        /// <summary>
        /// Peek at the tear for a side effect without altering the ritual.
        /// If the ritual is flowing, <paramref name="action"/> is never invoked.
        /// </summary>
        public static Ritual<T> ScryTear<T>(this Ritual<T> ritual, Action<Tear> action)
        {
            if (ritual.IsTorn) action(ritual.GetTear()!);
            return ritual;
        }

        /// <summary>
        /// Peek at the tear asynchronously for a side effect without altering the ritual.
        /// If the ritual is flowing, <paramref name="action"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<T>> ScryTearAsync<T>(
            this Ritual<T> ritual, Func<Tear, Task> action)
        {
            if (ritual.IsTorn) await action(ritual.GetTear()!);
            return ritual;
        }

        /// <summary>
        /// Await a ritual task, then peek at the tear asynchronously for a side effect
        /// without altering the ritual. If the ritual is flowing, <paramref name="action"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<T>> ScryTearAsync<T>(
            this Task<Ritual<T>> ritualTask, Func<Tear, Task> action)
            => await (await ritualTask).ScryTearAsync(action);
    }
}
