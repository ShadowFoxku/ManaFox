namespace ManaFox.Databases.Core.Attributes
{
    /// <summary>
    /// Marks this as to be ignored by the database on read/write operations
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreRuneAttribute : Attribute
    {
    }
}
