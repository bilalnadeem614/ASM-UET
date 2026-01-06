-- ========================================
-- Teacher Login Test User Creation
-- ========================================

USE ASM;
GO

-- Create a test teacher user for authentication testing
-- Password: Teacher@123
-- Base64 Hash: VGVhY2hlckAxMjM=

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'teacher@uet.edu.pk')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
    VALUES 
    (
        'Test Teacher',
        'teacher@uet.edu.pk',
        'VGVhY2hlckAxMjM=', -- Base64 of "Teacher@123"
        1, -- Teacher role
        GETDATE()
    );
    PRINT '? Test teacher user created successfully!';
    PRINT 'Email: teacher@uet.edu.pk';
    PRINT 'Password: Teacher@123';
END
ELSE
BEGIN
    PRINT '?? Test teacher user already exists.';
    
    -- Update password and role if needed
    UPDATE Users 
    SET PasswordHash = 'VGVhY2hlckAxMjM=',
        Role = 1
    WHERE Email = 'teacher@uet.edu.pk';
    
    PRINT '? Test teacher password and role updated.';
END
GO

-- Create some test courses assigned to the teacher
DECLARE @TeacherUserId INT = (SELECT UserId FROM Users WHERE Email = 'teacher@uet.edu.pk');

IF @TeacherUserId IS NOT NULL
BEGIN
    -- Insert test courses
    IF NOT EXISTS (SELECT 1 FROM Courses WHERE CourseCode = 'CS101')
    BEGIN
        INSERT INTO Courses (CourseCode, CourseName, TeacherId)
        VALUES ('CS101', 'Introduction to Computer Science', @TeacherUserId);
        PRINT '? Course CS101 created and assigned to teacher.';
    END

    IF NOT EXISTS (SELECT 1 FROM Courses WHERE CourseCode = 'CS201')
    BEGIN
        INSERT INTO Courses (CourseCode, CourseName, TeacherId)
        VALUES ('CS201', 'Data Structures and Algorithms', @TeacherUserId);
        PRINT '? Course CS201 created and assigned to teacher.';
    END
END

-- Create some test student users if they don't exist
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'student1@uet.edu.pk')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
    VALUES ('John Doe', 'student1@uet.edu.pk', 'U3R1ZGVudEAxMjM=', 2, GETDATE());
    PRINT '? Test student 1 created.';
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'student2@uet.edu.pk')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
    VALUES ('Jane Smith', 'student2@uet.edu.pk', 'U3R1ZGVudEAxMjM=', 2, GETDATE());
    PRINT '? Test student 2 created.';
END

-- Enroll students in courses
DECLARE @Student1Id INT = (SELECT UserId FROM Users WHERE Email = 'student1@uet.edu.pk');
DECLARE @Student2Id INT = (SELECT UserId FROM Users WHERE Email = 'student2@uet.edu.pk');
DECLARE @Course1Id INT = (SELECT CourseId FROM Courses WHERE CourseCode = 'CS101');
DECLARE @Course2Id INT = (SELECT CourseId FROM Courses WHERE CourseCode = 'CS201');

-- Enroll student 1 in both courses
IF @Student1Id IS NOT NULL AND @Course1Id IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Enrollments WHERE StudentId = @Student1Id AND CourseId = @Course1Id)
    BEGIN
        INSERT INTO Enrollments (StudentId, CourseId, EnrollmentDate)
        VALUES (@Student1Id, @Course1Id, GETDATE());
        PRINT '? Student 1 enrolled in CS101.';
    END
END

IF @Student1Id IS NOT NULL AND @Course2Id IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Enrollments WHERE StudentId = @Student1Id AND CourseId = @Course2Id)
    BEGIN
        INSERT INTO Enrollments (StudentId, CourseId, EnrollmentDate)
        VALUES (@Student1Id, @Course2Id, GETDATE());
        PRINT '? Student 1 enrolled in CS201.';
    END
END

-- Enroll student 2 in CS101
IF @Student2Id IS NOT NULL AND @Course1Id IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Enrollments WHERE StudentId = @Student2Id AND CourseId = @Course1Id)
    BEGIN
        INSERT INTO Enrollments (StudentId, CourseId, EnrollmentDate)
        VALUES (@Student2Id, @Course1Id, GETDATE());
        PRINT '? Student 2 enrolled in CS101.';
    END
END

-- Verify the setup
SELECT 
    u.UserId,
    u.FullName,
    u.Email,
    u.Role,
    CASE u.Role
        WHEN 0 THEN 'Admin'
        WHEN 1 THEN 'Teacher'
        WHEN 2 THEN 'Student'
    END AS RoleName,
    u.CreatedAt
FROM Users u
WHERE u.Email IN ('teacher@uet.edu.pk', 'student1@uet.edu.pk', 'student2@uet.edu.pk')
ORDER BY u.Role, u.FullName;

-- Show courses assigned to teacher
SELECT 
    c.CourseId,
    c.CourseCode,
    c.CourseName,
    t.FullName AS TeacherName,
    COUNT(e.EnrollmentId) AS EnrollmentCount
FROM Courses c
INNER JOIN Users t ON c.TeacherId = t.UserId
LEFT JOIN Enrollments e ON c.CourseId = e.CourseId
WHERE t.Email = 'teacher@uet.edu.pk'
GROUP BY c.CourseId, c.CourseCode, c.CourseName, t.FullName;

PRINT '';
PRINT '========================================';
PRINT '?? TEST CREDENTIALS';
PRINT '========================================';
PRINT '';
PRINT '????? Teacher Account:';
PRINT '   Email: teacher@uet.edu.pk';
PRINT '   Password: Teacher@123';
PRINT '';
PRINT '????? Student Accounts:';
PRINT '   Email: student1@uet.edu.pk / student2@uet.edu.pk';
PRINT '   Password: Student@123';
PRINT '';
PRINT '========================================';