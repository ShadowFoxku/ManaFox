using ManaFox.Core.Errors;
using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow
{
    public static class RitualTransformationExtensions
    {
        /// <summary>
        /// Transform the flowing value into a new type, leaving tears untouched (functor map).
        /// </summary>
        public static Ritual<TMapped> Map<T, TMapped>(
            this Ritual<T> ritual,
            Func<T, TMapped> map)
            => ritual.Match(
                onLeft: tear => Ritual<TMapped>.Tear(tear),
                onRight: value => Ritual<TMapped>.Flow(map(value)));

        /// <summary>
        /// Chain an operation that can itself fail (monad bind). If the ritual is torn,
        /// the tear is propagated and <paramref name="bind"/> is never invoked.
        /// </summary>
        public static Ritual<TMapped> Bind<T, TMapped>(
            this Ritual<T> ritual,
            Func<T, Ritual<TMapped>> bind)
            => ritual.Match(
                onLeft: tear => Ritual<TMapped>.Tear(tear),
                onRight: bind);

        /// <summary>
        /// Chain an async operation that can itself fail. If the ritual is torn,
        /// the tear is propagated and <paramref name="bind"/> is never invoked.
        /// </summary>
        public static Task<Ritual<TMapped>> BindAsync<T, TMapped>(
            this Ritual<T> ritual,
            Func<T, Task<Ritual<TMapped>>> bind)
            => ritual.Match(
                onLeft: tear => Task.FromResult(Ritual<TMapped>.Tear(tear)),
                onRight: bind);

        /// <summary>
        /// Await a ritual task, then chain a synchronous operation that can itself fail.
        /// If the ritual is torn, the tear is propagated and <paramref name="bind"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<TMapped>> BindAsync<T, TMapped>(
            this Task<Ritual<T>> ritualTask,
            Func<T, Ritual<TMapped>> bind)
            => (await ritualTask).Bind(bind);

        /// <summary>
        /// Await a ritual task, then chain an async operation that can itself fail.
        /// If the ritual is torn, the tear is propagated and <paramref name="bind"/> is never invoked.
        /// </summary>
        public static async Task<Ritual<TMapped>> BindAsync<T, TMapped>(
            this Task<Ritual<T>> ritualTask,
            Func<T, Task<Ritual<TMapped>>> bind)
            => await (await ritualTask).BindAsync(bind);

        /// <summary>
        /// Transform the tear, leaving flowing values untouched. Useful for adding context
        /// to errors as they propagate up the chain.
        /// </summary>
        public static Ritual<T> MapTear<T>(
            this Ritual<T> ritual,
            Func<Tear, Tear> onTear)
            => ritual.Match(
                onLeft: tear => Ritual<T>.Tear(onTear(tear)),
                onRight: Ritual<T>.Flow);

        /// <summary>
        /// Await a ritual task, then transform its tear if torn, leaving flowing values untouched.
        /// </summary>
        public static async Task<Ritual<T>> MapTearAsync<T>(
            this Task<Ritual<T>> ritualTask,
            Func<Tear, Tear> onTear)
            => (await ritualTask).MapTear(onTear);

        /// <summary>
        /// Transform both sides of a ritual simultaneously — the flowing value into a new type,
        /// and the tear into a new tear.
        /// </summary>
        public static Ritual<TMapped> BiMap<T, TMapped>(
            this Ritual<T> ritual,
            Func<Tear, Tear> onTear,
            Func<T, TMapped> onFlow)
            => ritual.Match(
                onLeft: tear => Ritual<TMapped>.Tear(onTear(tear)),
                onRight: value => Ritual<TMapped>.Flow(onFlow(value)));

        /// <summary>
        /// Await a ritual task, then transform both sides simultaneously.
        /// </summary>
        public static async Task<Ritual<TMapped>> BiMapAsync<T, TMapped>(
            this Task<Ritual<T>> ritualTask,
            Func<Tear, Tear> onTear,
            Func<T, TMapped> onFlow)
            => (await ritualTask).BiMap(onTear, onFlow);

        /// <summary>
        /// Assert a condition on a flowing value. If the predicate returns <see langword="false"/>,
        /// the ritual becomes torn with <paramref name="tearMessage"/>. Torn rituals pass through unchanged.
        /// </summary>
        public static Ritual<T> Ensure<T>(
            this Ritual<T> ritual,
            Func<T, bool> predicate,
            string tearMessage)
            => ritual.Bind(value => predicate(value)
                ? Ritual<T>.Flow(value)
                : Ritual<T>.Tear(tearMessage));

        /// <summary>
        /// Await a ritual task, then assert a condition on the flowing value. If the predicate
        /// returns <see langword="false"/>, the ritual becomes torn with <paramref name="tearMessage"/>.
        /// Torn rituals pass through unchanged.
        /// </summary>
        public static async Task<Ritual<T>> EnsureAsync<T>(
            this Task<Ritual<T>> ritualTask,
            Func<T, bool> predicate,
            string tearMessage)
            => (await ritualTask).Ensure(predicate, tearMessage);
    }
}
