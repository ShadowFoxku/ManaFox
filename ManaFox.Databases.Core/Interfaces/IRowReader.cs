using System.Data;

namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRowReader : IDataReader
    {
        bool HasColumn(string name, out int ordinal);
    }
}
