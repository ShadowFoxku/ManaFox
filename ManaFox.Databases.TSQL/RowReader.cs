using ManaFox.Databases.Core.Interfaces;
using Microsoft.Data.SqlClient;
using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace ManaFox.Databases.TSQL
{
    public class RowReader(SqlDataReader underlying) : DbDataReader, IRowReader, IAsyncDisposable
    {
        private readonly SqlDataReader Underlying = underlying;

        public override async ValueTask DisposeAsync()
        {
            await Underlying.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public static RowReader For(SqlDataReader r) => new(r);

        public bool HasColumn(string name, out int ordinal)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);

            try
            {
                ordinal = GetOrdinal(name);
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                ordinal = -1;
                return false;
            }
        }

        public override object this[int ordinal] => Underlying[ordinal];

        public override object this[string name] => Underlying[name];

        public override int Depth => Underlying.Depth;

        public override int FieldCount => Underlying.FieldCount;

        public override bool HasRows => Underlying.HasRows;

        public override bool IsClosed => Underlying.IsClosed;

        public override int RecordsAffected => Underlying.RecordsAffected;

        public override bool GetBoolean(int ordinal) => Underlying.GetBoolean(ordinal);

        public override byte GetByte(int ordinal) => Underlying.GetByte(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => Underlying.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        public override char GetChar(int ordinal) => Underlying.GetChar(ordinal);

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => Underlying.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        public override string GetDataTypeName(int ordinal) => Underlying.GetDataTypeName(ordinal);

        public override DateTime GetDateTime(int ordinal) => Underlying.GetDateTime(ordinal);

        public override decimal GetDecimal(int ordinal) => Underlying.GetDecimal(ordinal);

        public override double GetDouble(int ordinal) => Underlying.GetDouble(ordinal);

        public override IEnumerator GetEnumerator() => Underlying.GetEnumerator();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int ordinal) => Underlying.GetFieldType(ordinal);

        public override float GetFloat(int ordinal) => Underlying.GetFloat(ordinal);

        public override Guid GetGuid(int ordinal) => Underlying.GetGuid(ordinal);

        public override short GetInt16(int ordinal) => Underlying.GetInt16(ordinal);

        public override int GetInt32(int ordinal) => Underlying.GetInt32(ordinal);

        public override long GetInt64(int ordinal) => Underlying.GetInt64(ordinal);

        public override string GetName(int ordinal) => Underlying.GetName(ordinal);

        public override int GetOrdinal(string name) => Underlying.GetOrdinal(name);

        public override string GetString(int ordinal) => Underlying.GetString(ordinal);

        public override object GetValue(int ordinal) => Underlying.GetValue(ordinal);

        public override int GetValues(object[] values) => Underlying.GetValues(values);

        public override bool IsDBNull(int ordinal) => Underlying.IsDBNull(ordinal);

        public override bool NextResult() => Underlying.NextResult();

        public override bool Read() => Underlying.Read();
    }
}
