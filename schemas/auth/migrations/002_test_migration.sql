-- Test migration — kept as a schema-aware T-SQL example
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[auth].[migration_test]') AND type = N'U')
BEGIN
    CREATE TABLE auth.migration_test (
        id INT IDENTITY(1,1) PRIMARY KEY,
        note NVARCHAR(50)
    );
END