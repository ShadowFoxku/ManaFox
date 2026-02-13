using ManaFox.Databases.TSQL.Interfaces;

namespace ManaFox.Databases.TSQL.Models
{
    public class RuneReaderConfiguration : IRuneReaderConfiguration
    {
        private const string ImplicitDefaultConnectionStringName = "Default";
        public RuneReaderConfiguration(string defaultConnectionStringName, IReadOnlyDictionary<string, string> connectionStrings)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(defaultConnectionStringName);
            ArgumentNullException.ThrowIfNull(connectionStrings);

            if (connectionStrings.Count == 0)
                throw new InvalidOperationException("At least one connection string must be provided.");

            Configure(defaultConnectionStringName, connectionStrings);
        }

        public RuneReaderConfiguration(IReadOnlyDictionary<string, string> connectionStrings) 
        {
            ArgumentNullException.ThrowIfNull(connectionStrings);

            if (connectionStrings.Count == 0)
                throw new InvalidOperationException("At least one connection string must be provided.");

            var defaultName = connectionStrings.ContainsKey(ImplicitDefaultConnectionStringName)
                ? ImplicitDefaultConnectionStringName
                : connectionStrings.Keys.First();

            Configure(defaultName, connectionStrings);
        }

        private void Configure(string defaultConnectionStringName, IReadOnlyDictionary<string, string> connectionStrings)
        {
            DefaultConnectionStringName = defaultConnectionStringName;
            ConnectionStrings = connectionStrings;
        }

        private string DefaultConnectionStringName { get; set; } = null!;

        private IReadOnlyDictionary<string, string> ConnectionStrings { get; set; } = null!;

        public string GetString(string name)
        {
            if (!ConnectionStrings.TryGetValue(name, out var value))
                throw new KeyNotFoundException($"Connection '{name}' was not found.");

            return value;
        }

        public string GetDefaultString()
        {
            return GetString(DefaultConnectionStringName);
        }

        public bool TryGetString(string name, out string? value)
        {
            return ConnectionStrings.TryGetValue(name, out value);
        }

        public bool TryGetDefaultString(out string? value)
        {
            return ConnectionStrings.TryGetValue(DefaultConnectionStringName, out value);
        }
    }
}
