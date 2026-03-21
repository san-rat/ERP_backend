-- Just a placeholder for now
-- Later we will add the actual migrations in the next migration files
-- Make sure on the next migration files to delete this code

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'analytics')
BEGIN
    EXEC('CREATE SCHEMA [analytics]');
END