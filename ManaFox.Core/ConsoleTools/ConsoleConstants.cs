namespace ManaFox.Core.ConsoleTools
{
    public class ConsoleConstants
    {
        #region Foreground
        public const string Black = "\e[30m";
        public const string Red = "\e[31m";
        public const string Green = "\e[32m";
        public const string Yellow = "\e[33m";
        public const string Blue = "\e[34m";
        public const string Magenta = "\e[35m";
        public const string Cyan = "\e[36m";
        public const string White = "\e[37m";

        public const string BrightBlack = "\e[90m";
        public const string BrightRed = "\e[91m";
        public const string BrightGreen = "\e[92m";
        public const string BrightYellow = "\e[93m";
        public const string BrightBlue = "\e[94m";
        public const string BrightMagenta = "\e[95m";
        public const string BrightCyan = "\e[96m";
        public const string BrightWhite = "\e[97m";
        #endregion Foreground

        #region Background
        public const string BgBlack = "\e[40m";
        public const string BgRed = "\e[41m";
        public const string BgGreen = "\e[42m";
        public const string BgYellow = "\e[43m";
        public const string BgBlue = "\e[44m";
        public const string BgMagenta = "\e[45m";
        public const string BgCyan = "\e[46m";
        public const string BgWhite = "\e[47m";

        public const string BgBrightBlack = "\e[100m";
        public const string BgBrightRed = "\e[101m";
        public const string BgBrightGreen = "\e[102m";
        public const string BgBrightYellow = "\e[103m";
        public const string BgBrightBlue = "\e[104m";
        public const string BgBrightMagenta = "\e[105m";
        public const string BgBrightCyan = "\e[106m";
        public const string BgBrightWhite = "\e[107m";
        #endregion Background

        #region Text styles
        public const string Reset = "\e[0m";
        public const string Bold = "\e[1m";
        public const string Dim = "\e[2m";
        public const string Italic = "\e[3m";
        public const string Underline = "\e[4m";
        public const string Strikethrough = "\e[9m";
        #endregion Text styles

        #region Custom colours
        /// <summary>Foreground from RGB values.</summary>
        public static string FgRgb(int r, int g, int b) => $"\e[38;2;{r};{g};{b}m";
        /// <summary>Background from RGB values.</summary>
        public static string BgRgb(int r, int g, int b) => $"\e[48;2;{r};{g};{b}m";
        #endregion Custom colours

        #region Cursors
        public const string HideCursor = "\e[?25l";
        public const string ShowCursor = "\e[?25h";
        public const string ClearLine = "\e[2K";
        public const string ClearToEOL = "\e[0K";

        public static string CursorUp(int n = 1) => $"\e[{n}A";
        public static string CursorCol(int n = 1) => $"\e[{n}G";
        #endregion Cursors
    }
}
