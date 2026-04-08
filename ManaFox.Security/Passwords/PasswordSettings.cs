namespace ManaFox.Security.Passwords
{    
    /// <summary>
    /// Make sure to check Argon2id before changing and tweaking these settings! These are a safe default, if you find it's taking a long time
    /// it may be worth adjusting, but otherwise these should be fine for most use cases. The defaults are set to be reasonably secure while 
    /// still being performant on modern hardware. Adjusting these settings can increase security but may also increase the time it takes to 
    /// hash passwords, so it's important to find a balance that works for your application and user base.
    /// </summary>
    public class PasswordSettings
    {
        public int DegreeOfParallelism { get; set; } = 2;
        public int Iterations { get; set; } = 4;
        public int MemorySize { get; set; } = 65536; // 64mb

        /// <summary>
        /// This should be a base64 encoded string. 
        /// If you set the pepper, it is HIGHLY recommended to set it to a value that is not stored in the same place as the rest of the configuration, 
        /// such as an environment variable or a secret manager. The pepper adds an additional layer of security by being a secret value that is combined 
        /// with the password before hashing, making it more resistant to attacks even if the database is compromised. 
        /// Changing the pepper will invalidate all existing password hashes, so it should be set carefully and not changed frequently. 
        /// If you do need to change it, make sure to have a plan for rehashing existing passwords with the new pepper.
        /// </summary>
        public string Pepper { get; set; } = string.Empty;

        public bool Matches(PasswordSettings other)
        {
            return DegreeOfParallelism == other.DegreeOfParallelism &&
                   Iterations == other.Iterations &&
                   MemorySize == other.MemorySize;
        }
    }
}
