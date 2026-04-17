-- Migration 003: Seed 50 Customers with GUIDs and 07-series Phones
BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Clear previous demo data (optional, based on the specific GUID range)
    DELETE FROM dbo.customers 
    WHERE id BETWEEN '550e8400-e29b-41d4-a716-446655440000' AND '550e8400-e29b-41d4-a716-446655440032';

    -- 2. Seed 50 customers manually
    ;WITH seed_data AS (
        SELECT *
        FROM (VALUES
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440000'), N'john.doe@example.com', N'John', N'Doe', N'0712345678'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440001'), N'jane.smith@example.com', N'Jane', N'Smith', N'0723456789'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440002'), N'alice.johnson@example.com', N'Alice', N'Johnson', N'0734567890'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440003'), N'bob.williams@example.com', N'Bob', N'Williams', N'0745678901'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440004'), N'carol.davis@example.com', N'Carol', N'Davis', N'0756789012'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440005'), N'david.miller@example.com', N'David', N'Miller', N'0767890123'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440006'), N'emma.wilson@example.com', N'Emma', N'Wilson', N'0778901234'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440007'), N'frank.moore@example.com', N'Frank', N'Moore', N'0789012345'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440008'), N'grace.taylor@example.com', N'Grace', N'Taylor', N'0790123456'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440009'), N'henry.anderson@example.com', N'Henry', N'Anderson', N'0701234567'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440010'), N'isaac.thomas@example.com', N'Isaac', N'Thomas', N'0711223344'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440011'), N'jack.white@example.com', N'Jack', N'White', N'0722334455'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440012'), N'katherine.harris@example.com', N'Katherine', N'Harris', N'0733445566'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440013'), N'liam.martin@example.com', N'Liam', N'Martin', N'0744556677'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440014'), N'mia.thompson@example.com', N'Mia', N'Thompson', N'0755667788'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440015'), N'noah.garcia@example.com', N'Noah', N'Garcia', N'0766778899'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440016'), N'olivia.martinez@example.com', N'Olivia', N'Martinez', N'0777889900'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440017'), N'paul.robinson@example.com', N'Paul', N'Robinson', N'0788990011'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440018'), N'quinn.clark@example.com', N'Quinn', N'Clark', N'0799001122'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440019'), N'ryan.rodriguez@example.com', N'Ryan', N'Rodriguez', N'0700112233'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440020'), N'sophia.lewis@example.com', N'Sophia', N'Lewis', N'0715151515'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440021'), N'thomas.lee@example.com', N'Thomas', N'Lee', N'0726262626'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440022'), N'ursula.walker@example.com', N'Ursula', N'Walker', N'0737373737'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440023'), N'victor.hall@example.com', N'Victor', N'Hall', N'0748484848'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440024'), N'wendy.allen@example.com', N'Wendy', N'Allen', N'0759595959'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440025'), N'xavier.young@example.com', N'Xavier', N'Young', N'0760606060'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440026'), N'yvonne.king@example.com', N'Yvonne', N'King', N'0771717171'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440027'), N'zachary.wright@example.com', N'Zachary', N'Wright', N'0782828282'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440028'), N'amber.scott@example.com', N'Amber', N'Scott', N'0793939393'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440029'), N'ben.green@example.com', N'Ben', N'Green', N'0704040404'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440030'), N'clara.baker@example.com', N'Clara', N'Baker', N'0716161616'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440031'), N'danny.adams@example.com', N'Danny', N'Adams', N'0727272727'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440032'), N'elena.nelson@example.com', N'Elena', N'Nelson', N'0738383838'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440033'), N'felix.hill@example.com', N'Felix', N'Hill', N'0749494949'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440034'), N'gina.ramirez@example.com', N'Gina', N'Ramirez', N'0750505050'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440035'), N'harvey.campbell@example.com', N'Harvey', N'Campbell', N'0761616161'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440036'), N'iris.mitchell@example.com', N'Iris', N'Mitchell', N'0772727272'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440037'), N'jake.roberts@example.com', N'Jake', N'Roberts', N'0783838383'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440038'), N'kara.carter@example.com', N'Kara', N'Carter', N'0794949494'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440039'), N'leo.phillips@example.com', N'Leo', N'Phillips', N'0705050505'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440040'), N'maya.evans@example.com', N'Maya', N'Evans', N'0717171717'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440041'), N'nathan.turner@example.com', N'Nathan', N'Turner', N'0728282828'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440042'), N'olga.torres@example.com', N'Olga', N'Torres', N'0739393939'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440043'), N'peter.parker@example.com', N'Peter', N'Parker', N'0740404040'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440044'), N'quinn.fabray@example.com', N'Quinn', N'Fabray', N'0751515151'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440045'), N'riley.reid@example.com', N'Riley', N'Reid', N'0762626262'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440046'), N'steven.strange@example.com', N'Steven', N'Strange', N'0773737373'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440047'), N'tessa.thompson@example.com', N'Tessa', N'Thompson', N'0784848484'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440048'), N'victor.von@example.com', N'Victor', N'Von', N'0795959595'),
            (CONVERT(UNIQUEIDENTIFIER, '550e8400-e29b-41d4-a716-446655440049'), N'wanda.maximoff@example.com', N'Wanda', N'Maximoff', N'0706060606')
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
        SELECT 1 FROM dbo.customers c WHERE c.email = s.email OR c.id = s.id
    );

    COMMIT TRANSACTION;
    PRINT 'Successfully seeded 50 customers with UNIQUEIDENTIFIER IDs.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH