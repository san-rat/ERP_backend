-- Migration 002: Seed demo customers

;WITH seed_data AS (
    SELECT *
    FROM (VALUES
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440000'), N'john.doe@example.com', N'John', N'Doe', N'555-1234'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440001'), N'jane.smith@example.com', N'Jane', N'Smith', N'555-5678'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440002'), N'alice.johnson@example.com', N'Alice', N'Johnson', N'555-9012'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440003'), N'bob.williams@example.com', N'Bob', N'Williams', N'555-3456'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440004'), N'carol.davis@example.com', N'Carol', N'Davis', N'555-7890'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440005'), N'david.miller@example.com', N'David', N'Miller', N'555-2341'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440006'), N'emma.wilson@example.com', N'Emma', N'Wilson', N'555-6789'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440007'), N'frank.moore@example.com', N'Frank', N'Moore', N'555-0123'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440008'), N'grace.taylor@example.com', N'Grace', N'Taylor', N'555-4567'),
        (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440009'), N'henry.anderson@example.com', N'Henry', N'Anderson', N'555-8901')
    ) AS v(id, email, first_name, last_name, phone)
)
INSERT INTO dbo.customers (id, email, first_name, last_name, phone, created_at, updated_at)
SELECT
    s.id,
    s.email,
    s.first_name,
    s.last_name,
    s.phone,
    GETUTCDATE(),
    GETUTCDATE()
FROM seed_data s
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.customers c
    WHERE c.id = s.id OR c.email = s.email
);
