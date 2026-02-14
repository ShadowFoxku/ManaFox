namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRuneReaderConfiguration
    {
        public string GetString(string name);
        public string GetDefaultString();

        public bool TryGetString(string name, out string? value);
        public bool TryGetDefaultString(out string? value);
    }
}
