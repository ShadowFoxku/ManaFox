namespace ManaFox.Databases.TSQL.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RuneNameAttribute(string columnName) : Attribute
    {
        public string ColumnName { get; private set; } = columnName;
    }
}
