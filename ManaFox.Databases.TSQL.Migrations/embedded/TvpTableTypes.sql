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