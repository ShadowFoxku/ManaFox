namespace ManaFox.Databases.Migrations;

/// <summary>
/// Exception thrown when database migration fails
/// </summary>
public class MigrationException : Exception
{
    public MigrationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}