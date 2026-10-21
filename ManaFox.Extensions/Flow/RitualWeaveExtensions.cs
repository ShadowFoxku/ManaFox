using ManaFox.Core.Flow;

namespace ManaFox.Extensions.Flow
{
    public static class RitualWeaveExtensions
    {
        /// <summary>
        /// Await two ritual tasks concurrently and combine their values into a tuple. If either is torn,
        /// the first torn ritual by argument order wins.
        /// </summary>
        public static async Task<Ritual<(T1, T2)>> WeaveAsync<T1, T2>(this Task<Ritual<T1>> ritual1, Task<Ritual<T2>> ritual2)
        {
            await Task.WhenAll(ritual1, ritual2);

            var r1 = ritual1.Result;
            var r2 = ritual2.Result;

            if (r1.IsTorn) return Ritual<(T1, T2)>.Tear(r1.GetTear()!);
            if (r2.IsTorn) return Ritual<(T1, T2)>.Tear(r2.GetTear()!);

            return Ritual<(T1, T2)>.Flow((r1.GetValue()!, r2.GetValue()!));
        }

        /// <summary>
        /// Weave another ritual onto an already woven tuple, flattening the result.
        /// </summary>
        public static Task<Ritual<(T1, T2, T3)>> WeaveAsync<T1, T2, T3>(
            this Task<Ritual<(T1, T2)>> ritual1, Task<Ritual<T3>> ritual2)
            => ritual1.WeaveAsync<(T1, T2), T3>(ritual2)
                .MapAsync(r => (r.Item1.Item1, r.Item1.Item2, r.Item2));

        /// <inheritdoc cref="WeaveAsync{T1, T2, T3}"/>
        public static Task<Ritual<(T1, T2, T3, T4)>> WeaveAsync<T1, T2, T3, T4>(
            this Task<Ritual<(T1, T2, T3)>> ritual1, Task<Ritual<T4>> ritual2)
            => ritual1.WeaveAsync<(T1, T2, T3), T4>(ritual2)
                .MapAsync(r => (r.Item1.Item1, r.Item1.Item2, r.Item1.Item3, r.Item2));

        /// <inheritdoc cref="WeaveAsync{T1, T2, T3}"/>
        public static Task<Ritual<(T1, T2, T3, T4, T5)>> WeaveAsync<T1, T2, T3, T4, T5>(
            this Task<Ritual<(T1, T2, T3, T4)>> ritual1, Task<Ritual<T5>> ritual2)
            => ritual1.WeaveAsync<(T1, T2, T3, T4), T5>(ritual2)
                .MapAsync(r => (r.Item1.Item1, r.Item1.Item2, r.Item1.Item3, r.Item1.Item4, r.Item2));

        /// <inheritdoc cref="WeaveAsync{T1, T2, T3}"/>
        public static Task<Ritual<(T1, T2, T3, T4, T5, T6)>> WeaveAsync<T1, T2, T3, T4, T5, T6>(
            this Task<Ritual<(T1, T2, T3, T4, T5)>> ritual1, Task<Ritual<T6>> ritual2)
            => ritual1.WeaveAsync<(T1, T2, T3, T4, T5), T6>(ritual2)
                .MapAsync(r => (r.Item1.Item1, r.Item1.Item2, r.Item1.Item3, r.Item1.Item4, r.Item1.Item5, r.Item2));

        /// <inheritdoc cref="WeaveAsync{T1, T2, T3}"/>
        public static Task<Ritual<(T1, T2, T3, T4, T5, T6, T7)>> WeaveAsync<T1, T2, T3, T4, T5, T6, T7>(
            this Task<Ritual<(T1, T2, T3, T4, T5, T6)>> ritual1, Task<Ritual<T7>> ritual2)
            => ritual1.WeaveAsync<(T1, T2, T3, T4, T5, T6), T7>(ritual2)
                .MapAsync(r => (r.Item1.Item1, r.Item1.Item2, r.Item1.Item3, r.Item1.Item4, r.Item1.Item5, r.Item1.Item6, r.Item2));

        /// <inheritdoc cref="WeaveAsync{T1, T2, T3}"/>
        public static Task<Ritual<(T1, T2, T3, T4, T5, T6, T7, T8)>> WeaveAsync<T1, T2, T3, T4, T5, T6, T7, T8>(
            this Task<Ritual<(T1, T2, T3, T4, T5, T6, T7)>> ritual1, Task<Ritual<T8>> ritual2)
            => ritual1.WeaveAsync<(T1, T2, T3, T4, T5, T6, T7), T8>(ritual2)
                .MapAsync(r => (r.Item1.Item1, r.Item1.Item2, r.Item1.Item3, r.Item1.Item4, r.Item1.Item5, r.Item1.Item6, r.Item1.Item7, r.Item2));
    }
}
