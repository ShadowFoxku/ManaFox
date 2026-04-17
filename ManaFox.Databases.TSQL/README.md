# ManaFox.Databases.TSQL

ManaFox.Databases.TSQL is a lightweight wrapper that sits atop the Microsoft.Data.SqlClient library to act as a basic wrapper that allows for both sync/async operations to be performed.

The following three types should be added to your project DACPAC to be able to use the default list types in mappings

```sql
IF NOT EXISTS (SELECT 1 FROM sys.table_types WHERE name = 'LongList' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TYPE dbo.LongList AS TABLE (Value BIGINT NOT NULL)
END

IF NOT EXISTS (SELECT 1 FROM sys.table_types WHERE name = 'IntList' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TYPE dbo.IntList AS TABLE (Value INT NOT NULL)
END

IF NOT EXISTS (SELECT 1 FROM sys.table_types WHERE name = 'StringList' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TYPE dbo.StringList AS TABLE (Value NVARCHAR(MAX) NOT NULL)
END
```
