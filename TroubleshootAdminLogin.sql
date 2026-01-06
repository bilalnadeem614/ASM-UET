-- ========================================
-- Admin Login Troubleshooting Script
-- ========================================

USE ASM;
GO

-- 1. Check if admin user exists
SELECT 
    UserId,
    FullName,
    Email,
    PasswordHash,
    Role,
    CreatedAt,
    LEN(PasswordHash) AS HashLength
FROM Users
WHERE Email = 'admin@uet.edu.pk';
GO

-- 2. If admin doesn't exist, create it with correct Base64 hash
-- Password: Admin@123
-- Base64 Hash: QWRtaW5AMTIz

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@uet.edu.pk')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
    VALUES 
    (
        'System Administrator',
        'admin@uet.edu.pk',
        'QWRtaW5AMTIz', -- Base64 of "Admin@123"
        0, -- Admin role
        GETDATE()
    );
    PRINT '? Admin user created successfully!';
    PRINT 'Email: admin@uet.edu.pk';
    PRINT 'Password: Admin@123';
END
ELSE
BEGIN
    PRINT '?? Admin user already exists.';
    
    -- Update password if needed
    UPDATE Users 
    SET PasswordHash = 'QWRtaW5AMTIz'
    WHERE Email = 'admin@uet.edu.pk';
    
    PRINT '?? Admin password updated to: Admin@123';
END
GO

-- 3. Verify admin user
SELECT 
    UserId,
    FullName,
    Email,
    PasswordHash,
    Role AS RoleId,
    CASE Role
        WHEN 0 THEN 'Admin'
        WHEN 1 THEN 'Teacher'
        WHEN 2 THEN 'Student'
    END AS RoleName,
    CreatedAt
FROM Users
WHERE Email = 'admin@uet.edu.pk';
GO

-- 4. Test password hash generation
DECLARE @Password NVARCHAR(100) = 'Admin@123';
DECLARE @ExpectedHash NVARCHAR(500) = 'QWRtaW5AMTIz';
DECLARE @ActualHash NVARCHAR(500);

SELECT @ActualHash = PasswordHash
FROM Users
WHERE Email = 'admin@uet.edu.pk';

IF @ActualHash = @ExpectedHash
    PRINT '? Password hash matches expected value!';
ELSE
BEGIN
    PRINT '? Password hash mismatch!';
    PRINT 'Expected: ' + @ExpectedHash;
    PRINT 'Actual: ' + ISNULL(@ActualHash, 'NULL');
END
GO

-- 5. Check all users with their roles
SELECT 
    UserId,
    FullName,
    Email,
    Role,
    CASE Role
        WHEN 0 THEN 'Admin'
        WHEN 1 THEN 'Teacher'
        WHEN 2 THEN 'Student'
    END AS RoleName,
    CreatedAt
FROM Users
ORDER BY Role, FullName;
GO

-- 6. Additional test users (optional - for testing)
/*
-- Create a test teacher
INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
VALUES ('Test Teacher', 'teacher@uet.edu.pk', 'VGVhY2hlckAxMjM=', 1, GETDATE());

-- Create a test student  
INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
VALUES ('Test Student', 'student@uet.edu.pk', 'U3R1ZGVudEAxMjM=', 2, GETDATE());
*/

PRINT '';
PRINT '========================================';
PRINT '?? LOGIN CREDENTIALS';
PRINT '========================================';
PRINT '';
PRINT '?? Admin Account:';
PRINT '   Email: admin@uet.edu.pk';
PRINT '   Password: Admin@123';
PRINT '';
PRINT '========================================';
