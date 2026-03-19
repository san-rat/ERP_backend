-- Migration 005: Reset and seed demo auth users
-- All seeded demo users use SHA-256("Admin@123") to match AuthController

IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'MANAGER')
BEGIN
    INSERT INTO auth.roles (role_name) VALUES ('MANAGER');
END
GO

DECLARE @sharedPasswordHash NVARCHAR(255) = 'e86f78a8a3caf0b60d8e74e5942aa6d86dc150cd3c03338aef25b7d2d7e3acc7';
DECLARE @adminRoleId INT;
DECLARE @userRoleId INT;
DECLARE @managerRoleId INT;
DECLARE @adminUserId UNIQUEIDENTIFIER = NULL;
DECLARE @testUserId UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '450e8400-e29b-41d4-a716-446655440000');
DECLARE @managerUserId UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '450e8400-e29b-41d4-a716-446655440001');

SELECT @adminRoleId = id FROM auth.roles WHERE role_name = 'ADMIN';
SELECT @userRoleId = id FROM auth.roles WHERE role_name = 'USER';
SELECT @managerRoleId = id FROM auth.roles WHERE role_name = 'MANAGER';

IF @adminRoleId IS NULL
BEGIN
    THROW 49999, 'ADMIN role must exist before resetting the demo admin user.', 1;
END

IF @userRoleId IS NULL
BEGIN
    THROW 50000, 'USER role must exist before seeding demo auth users.', 1;
END

IF @managerRoleId IS NULL
BEGIN
    THROW 50001, 'MANAGER role must exist before seeding demo auth users.', 1;
END

SELECT TOP 1 @adminUserId = u.id
FROM auth.users u
WHERE u.username = 'admin'
   OR u.email IN ('admin@insighterp.com', 'admin@insighterp.local')
ORDER BY CASE WHEN u.username = 'admin' THEN 0 ELSE 1 END, u.created_at;

IF @adminUserId IS NULL
BEGIN
    SET @adminUserId = CONVERT(UNIQUEIDENTIFIER, '450e8400-e29b-41d4-a716-446655440002');

    INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
    VALUES (
        @adminUserId,
        'admin',
        'admin@insighterp.local',
        @sharedPasswordHash,
        'System Admin',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE auth.users
    SET
        username = 'admin',
        email = 'admin@insighterp.local',
        password_hash = @sharedPasswordHash,
        full_name = 'System Admin',
        is_active = 1,
        updated_at = GETUTCDATE()
    WHERE id = @adminUserId;
END

DELETE FROM auth.user_roles
WHERE user_id = @adminUserId
  AND role_id <> @adminRoleId;

IF NOT EXISTS (SELECT 1 FROM auth.user_roles WHERE user_id = @adminUserId AND role_id = @adminRoleId)
BEGIN
    INSERT INTO auth.user_roles (user_id, role_id)
    VALUES (@adminUserId, @adminRoleId);
END

IF NOT EXISTS (SELECT 1 FROM auth.users WHERE username = 'testuser' OR email = 'testuser@insighterp.local')
BEGIN
    INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
    VALUES (
        @testUserId,
        'testuser',
        'testuser@insighterp.local',
        @sharedPasswordHash,
        'Test User',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
END
ELSE
BEGIN
    SELECT TOP 1 @testUserId = id
    FROM auth.users
    WHERE username = 'testuser' OR email = 'testuser@insighterp.local'
    ORDER BY CASE WHEN username = 'testuser' THEN 0 ELSE 1 END, created_at;
END

IF NOT EXISTS (SELECT 1 FROM auth.user_roles WHERE user_id = @testUserId AND role_id = @userRoleId)
BEGIN
    INSERT INTO auth.user_roles (user_id, role_id)
    VALUES (@testUserId, @userRoleId);
END

IF NOT EXISTS (SELECT 1 FROM auth.users WHERE username = 'manager' OR email = 'manager@insighterp.local')
BEGIN
    INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
    VALUES (
        @managerUserId,
        'manager',
        'manager@insighterp.local',
        @sharedPasswordHash,
        'Manager User',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
END
ELSE
BEGIN
    SELECT TOP 1 @managerUserId = id
    FROM auth.users
    WHERE username = 'manager' OR email = 'manager@insighterp.local'
    ORDER BY CASE WHEN username = 'manager' THEN 0 ELSE 1 END, created_at;
END

IF NOT EXISTS (SELECT 1 FROM auth.user_roles WHERE user_id = @managerUserId AND role_id = @managerRoleId)
BEGIN
    INSERT INTO auth.user_roles (user_id, role_id)
    VALUES (@managerUserId, @managerRoleId);
END
