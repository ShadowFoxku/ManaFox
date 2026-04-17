using System.Runtime.CompilerServices;

namespace ManaFox.Core.Flow
{
    public class AsyncRitual<T>(Task<Ritual<T>> inner)
    {
        private readonly Task<Ritual<T>> _inner = inner;

        public TaskAwaiter<T> GetAwaiter()
        {
            return UnwrapAsync().GetAwaiter();
        }

        private async Task<T> UnwrapAsync()
        {
            var ritual = await _inner;
            if (ritual.IsTorn)
            {
                var tear = ritual.GetTear();
                throw new InvalidOperationException($"Ritual is torn: {tear}");
            }
            return ritual.GetValue()!;
        }

        public Task<Ritual<T>> AsTask() => _inner;

        public static implicit operator Task<Ritual<T>>(AsyncRitual<T> r) => r._inner;
    }
}
