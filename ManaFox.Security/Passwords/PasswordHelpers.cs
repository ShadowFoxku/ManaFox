using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace ManaFox.Security.Passwords
{
    public static class PasswordHelpers
    {
        private const int HashSize = 32;
        private const int SaltSize = 16;

        public static string HashPassword(string password, PasswordSettings settings)
        {
            byte[] salt = GenerateSalt();
            byte[] hash = ComputeHash(password, salt, settings);
            return Serialize(hash, salt, settings);
        }

        public static (bool isValid, bool reHash) VerifyPassword(string password, string stored, PasswordSettings settings)
        {
            var (deserializedHash, deserializedSettings, salt) = Deserialize(stored);
            bool needsReHash = !settings.Matches(deserializedSettings);

            if (!string.IsNullOrWhiteSpace(settings.Pepper))
                deserializedSettings.Pepper = settings.Pepper;

            var computedHash = ComputeHash(password, salt, deserializedSettings);

            var isValid = CryptographicOperations.FixedTimeEquals(computedHash, deserializedHash);

            return (isValid, needsReHash);
        }

        private static byte[] ComputeHash(string password, byte[] salt, PasswordSettings settings)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = settings.DegreeOfParallelism,
                Iterations = settings.Iterations,
                MemorySize = settings.MemorySize,
            };

            if (!string.IsNullOrWhiteSpace(settings.Pepper))
                argon2.KnownSecret = Convert.FromBase64String(settings.Pepper);

            return argon2.GetBytes(HashSize);
        }

        private static string Serialize(byte[] password, byte[] salt, PasswordSettings settings)
        {
            var pwString = Convert.ToBase64String(password);
            var saltString = Convert.ToBase64String(salt);

            return $"$argon2id$v=19$m={settings.MemorySize},t={settings.Iterations},p={settings.DegreeOfParallelism}${saltString}${pwString}";
        }

        private static (byte[] password, PasswordSettings settingsUsed, byte[] salt) Deserialize(string body)
        {
            var parts = body.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || parts[0] != "argon2id")
                throw new FormatException("Invalid hash format.");

            var settingsPart = parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries);
            var settings = new PasswordSettings
            {
                MemorySize = int.Parse(settingsPart[0].Split('=')[1]),
                Iterations = int.Parse(settingsPart[1].Split('=')[1]),
                DegreeOfParallelism = int.Parse(settingsPart[2].Split('=')[1])
            };
            byte[] salt = Convert.FromBase64String(parts[3]);
            byte[] password = Convert.FromBase64String(parts[4]);
            return (password, settings, salt);
        }

        private static byte[] GenerateSalt()
        {
            return RandomNumberGenerator.GetBytes(SaltSize);
        }
    }
}
