-- Create auth schema if it does not exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    EXEC('CREATE SCHEMA [auth]');
END

-- Create users table in auth schema
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[auth].[users]') AND type = N'U')
BEGIN
    CREATE TABLE auth.users (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        email NVARCHAR(255) NOT NULL,
        password_hash NVARCHAR(255) NOT NULL,
        full_name NVARCHAR(255),
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE NONCLUSTERED INDEX uq_users_email ON auth.users(email);
END

-- Create roles table in auth schema
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[auth].[roles]') AND type = N'U')
BEGIN
    CREATE TABLE auth.roles (
        id INT IDENTITY(1,1) PRIMARY KEY,
        role_name NVARCHAR(50) NOT NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX uq_roles_name ON auth.roles(role_name);
END

-- Create user_roles join table in auth schema
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[auth].[user_roles]') AND type = N'U')
BEGIN
    CREATE TABLE auth.user_roles (
        user_id UNIQUEIDENTIFIER NOT NULL,
        role_id INT NOT NULL,
        PRIMARY KEY (user_id, role_id),
        CONSTRAINT fk_ur_user FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE,
        CONSTRAINT fk_ur_role FOREIGN KEY (role_id) REFERENCES auth.roles(id) ON DELETE CASCADE
    );
END

-- Insert initial roles if they do not exist
IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'ADMIN')
BEGIN
    INSERT INTO auth.roles (role_name) VALUES ('ADMIN');
END

IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'USER')
BEGIN
    INSERT INTO auth.roles (role_name) VALUES ('USER');
END