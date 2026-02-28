namespace ManaFox.Core.ConsoleTools
{
    public static class ManaConsole
    {
        public static void Init()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        public static bool IsTTY => !Console.IsOutputRedirected;

        /// <summary>
        /// Repeatedly calls writeFrame with an incrementing index until the
        /// CancellationToken is cancelled. Use \r at the start of each frame
        /// to overwrite the current line.
        ///
        /// Usage:
        ///
        ///   var cts = new CancellationTokenSource();
        ///
        ///   string[] frames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        ///   var loop = ManaConsole.AnimateAsync(
        ///       i => Console.Write($"\r  {ConsoleConstants.BrightCyan}{frames[i % frames.Length]}{ConsoleConstants.Reset}  Loading..."),
        ///       cts.Token
        ///   );
        ///
        ///   await DoYourWork();
        ///
        ///   cts.Cancel();
        ///   await loop;
        ///
        ///   Console.Write($"\r{ConsoleConstants.ClearLine}");
        ///   Console.WriteLine($"  {ConsoleConstants.BrightGreen}done{ConsoleConstants.Reset}");
        ///
        /// </summary>
        public static async Task AnimateAsync(Action<int> writeFrame, CancellationToken ct, int intervalMs = 80)
        {
            if (!IsTTY) return;

            Console.Write(ConsoleConstants.HideCursor);
            int i = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    writeFrame(i++);
                    await Task.Delay(intervalMs, ct);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                Console.Write(ConsoleConstants.ShowCursor);
            }
        }

        /// <summary>
        /// Creates the animation shown in usage of <seealso cref="AnimateAsync(Action{int}, CancellationToken, int)"/>. Use that version for custom frames!
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="intervalMs"></param>
        /// <returns></returns>
        public static Task AnimateAsync(CancellationToken ct, int intervalMs = 80)
        {
            string[] frames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            return AnimateAsync(i => Console.Write($"\r  {ConsoleConstants.BrightCyan}{frames[i % frames.Length]}{ConsoleConstants.Reset}  Loading..."),ct);
        }
    }
}
