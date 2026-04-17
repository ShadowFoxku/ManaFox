# ManaFox.Databases.TSQL

ManaFox.Databases.TSQL is a lightweight wrapper that sits atop the Microsoft.Data.SqlClient library to act as a basic wrapper that allows for both sync/async operations to be performed.

The following three types should be added to your project DACPAC to be able to use the default list types in mappings

```sql
CREATE TYPE [dbo].[IntList] AS TABLE
(
	Id INT
)
```

```sql
CREATE TYPE [dbo].[LongList] AS TABLE
(
	Id BIGINT
)

```

```sql
CREATE TYPE [dbo].[StringList] AS TABLE
(
	Id NVARCHAR(MAX)
)
```
