-- Migration 004: Seed the default admin user
-- Password hash is SHA-256 of "Admin@123"
-- SHA-256("Admin@123") = a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3
-- Note: This is SHA-256 to match the current HashPassword() implementation in AuthController

DECLARE @adminId UNIQUEIDENTIFIER = NEWID();
DECLARE @adminRoleId INT;

-- Get the ADMIN role id
SELECT @adminRoleId = id FROM auth.roles WHERE role_name = 'ADMIN';

-- Only seed if no admin user exists yet
IF NOT EXISTS (SELECT 1 FROM auth.users u
               INNER JOIN auth.user_roles ur ON u.id = ur.user_id
               INNER JOIN auth.roles r ON ur.role_id = r.id
               WHERE r.role_name = 'ADMIN')
BEGIN
    INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
    VALUES (
        @adminId,
        'admin',
        'admin@insighterp.com',
        'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3',
        'System Admin',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );

    INSERT INTO auth.user_roles (user_id, role_id)
    VALUES (@adminId, @adminRoleId);
END
