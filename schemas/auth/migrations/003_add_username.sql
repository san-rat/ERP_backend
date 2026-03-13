-- Migration 003: Add username column to auth.users
-- Username is used for login instead of email

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[auth].[users]') AND name = 'username'
)
BEGIN
    ALTER TABLE auth.users ADD username NVARCHAR(100) NULL;
END
GO

-- Create unique index on username (once column exists)
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[auth].[users]') AND name = 'uq_users_username'
)
BEGIN
    -- Fill any existing rows with a temp value so index can be created
    UPDATE auth.users SET username = CAST(id AS NVARCHAR(100)) WHERE username IS NULL;

    -- Make the column NOT NULL now that all rows have a value
    ALTER TABLE auth.users ALTER COLUMN username NVARCHAR(100) NOT NULL;

    CREATE UNIQUE NONCLUSTERED INDEX uq_users_username ON auth.users(username);
END
GO
