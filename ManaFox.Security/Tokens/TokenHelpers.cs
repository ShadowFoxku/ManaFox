using System.Security.Cryptography;

namespace ManaFox.Security.Tokens
{
    public static class TokenHelpers
    {
        public static (string token, string hash) GenerateNewToken(int size)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(size);
            var token = Convert.ToBase64String(tokenBytes);
            var hash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
            return (token, hash);
        }

        public static string HashToken(string token)
        {
            var tokenBytes = Convert.FromBase64String(token);
            return Convert.ToBase64String(SHA256.HashData(tokenBytes));
        }
    }
}
