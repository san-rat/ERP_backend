-- Migration 006: Seed canonical Employee role and demo employee user
-- Password hash is SHA-256 of "Employee@123"
-- SHA-256("Employee@123") = b4bd29480ab196faa782e0d4ecd10c2f4212814105227e5f7992f5bf4b212a64

IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'Employee')
BEGIN
    INSERT INTO auth.roles (role_name) VALUES ('Employee');
END

DECLARE @employeeRoleId INT;
DECLARE @employeeUserId UNIQUEIDENTIFIER = CONVERT(UNIQUEIDENTIFIER, '450e8400-e29b-41d4-a716-446655440003');
DECLARE @employeePasswordHash NVARCHAR(255) = 'b4bd29480ab196faa782e0d4ecd10c2f4212814105227e5f7992f5bf4b212a64';

SELECT @employeeRoleId = id
FROM auth.roles
WHERE role_name = 'Employee';

IF @employeeRoleId IS NULL
BEGIN
    THROW 50002, 'Employee role must exist before seeding the employee demo user.', 1;
END

IF EXISTS (SELECT 1 FROM auth.users WHERE username = 'employee' OR email = 'employee@insighterp.local')
BEGIN
    SELECT TOP 1 @employeeUserId = id
    FROM auth.users
    WHERE username = 'employee' OR email = 'employee@insighterp.local'
    ORDER BY CASE WHEN username = 'employee' THEN 0 ELSE 1 END, created_at;

    UPDATE auth.users
    SET
        username = 'employee',
        email = 'employee@insighterp.local',
        password_hash = @employeePasswordHash,
        full_name = 'Employee User',
        is_active = 1,
        updated_at = GETUTCDATE()
    WHERE id = @employeeUserId;
END
ELSE
BEGIN
    INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
    VALUES (
        @employeeUserId,
        'employee',
        'employee@insighterp.local',
        @employeePasswordHash,
        'Employee User',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
END

DELETE FROM auth.user_roles
WHERE user_id = @employeeUserId
  AND role_id <> @employeeRoleId;

IF NOT EXISTS (SELECT 1 FROM auth.user_roles WHERE user_id = @employeeUserId AND role_id = @employeeRoleId)
BEGIN
    INSERT INTO auth.user_roles (user_id, role_id)
    VALUES (@employeeUserId, @employeeRoleId);
END
