using Microsoft.EntityFrameworkCore;
using ASM_UET.Models;

namespace ASM_UET.Services
{
    public class StudentService : IStudentService
    {
        private readonly ASM _db;

        public StudentService(ASM db)
        {
            _db = db;
        }

        public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId)
        {
            try
            {
                // Get student details
                var student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == studentId && u.Role == 2);
                if (student == null)
                {
                    throw new Exception($"Student with ID {studentId} not found.");
                }

                // Get enrollment count
                var totalEnrollments = await _db.Enrollments
                    .CountAsync(e => e.StudentId == studentId);

                // Get attendance statistics across all courses
                var attendanceStats = await _db.Attendances
                    .Where(a => a.Enrollment.StudentId == studentId)
                    .GroupBy(a => 1) // Group all records together
                    .Select(g => new
                    {
                        TotalClasses = g.Count(),
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AbsentCount = g.Count(a => a.Status == "Absent"),
                        LateCount = g.Count(a => a.Status == "Late")
                    })
                    .FirstOrDefaultAsync();

                var totalClasses = attendanceStats?.TotalClasses ?? 0;
                var presentCount = attendanceStats?.PresentCount ?? 0;
                var absentCount = attendanceStats?.AbsentCount ?? 0;
                var lateCount = attendanceStats?.LateCount ?? 0;

                var overallAttendancePercentage = totalClasses > 0 
                    ? Math.Round((decimal)(presentCount + lateCount) / totalClasses * 100, 2)
                    : 0;

                // Get recent courses (top 3 most recent enrollments)
                var recentCourses = await GetStudentCoursesAsync(studentId);
                var recentCoursesLimited = recentCourses.Take(3).ToList();

                // Get recent attendance records (last 10 records)
                var recentAttendance = await _db.Attendances
                    .Where(a => a.Enrollment.StudentId == studentId)
                    .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Course)
                    .OrderByDescending(a => a.Date)
                    .Take(10)
                    .Select(a => new RecentAttendanceRecordDto
                    {
                        CourseCode = a.Enrollment.Course.CourseCode,
                        CourseName = a.Enrollment.Course.CourseName,
                        Date = a.Date,
                        Status = a.Status
                    })
                    .ToListAsync();

                return new StudentDashboardDto
                {
                    StudentId = student.UserId,
                    StudentName = student.FullName,
                    Email = student.Email,
                    TotalEnrollments = totalEnrollments,
                    OverallAttendancePercentage = overallAttendancePercentage,
                    TotalClasses = totalClasses,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LateCount = lateCount,
                    RecentCourses = recentCoursesLimited,
                    RecentAttendance = recentAttendance
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving student dashboard: {ex.Message}", ex);
            }
        }

        public async Task<List<StudentCourseDto>> GetStudentCoursesAsync(int studentId)
        {
            try
            {
                var courses = await _db.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                    .Include(e => e.Attendances)
                    .Select(e => new
                    {
                        e.Course.CourseId,
                        e.Course.CourseCode,
                        e.Course.CourseName,
                        TeacherName = e.Course.Teacher.FullName,
                        e.EnrollmentDate,
                        TotalClasses = e.Attendances.Count(),
                        PresentCount = e.Attendances.Count(a => a.Status == "Present"),
                        AbsentCount = e.Attendances.Count(a => a.Status == "Absent"),
                        LateCount = e.Attendances.Count(a => a.Status == "Late")
                    })
                    .ToListAsync();

                return courses.Select(c => new StudentCourseDto
                {
                    CourseId = c.CourseId,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    TeacherName = c.TeacherName,
                    EnrollmentDate = c.EnrollmentDate,
                    TotalClasses = c.TotalClasses,
                    PresentCount = c.PresentCount,
                    AbsentCount = c.AbsentCount,
                    LateCount = c.LateCount,
                    AttendancePercentage = c.TotalClasses > 0 
                        ? Math.Round((decimal)(c.PresentCount + c.LateCount) / c.TotalClasses * 100, 2)
                        : 0
                }).OrderByDescending(c => c.EnrollmentDate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving student courses: {ex.Message}", ex);
            }
        }

        public async Task<List<AvailableCourseDto>> GetAvailableCoursesAsync(int studentId)
        {
            try
            {
                // Get course IDs that student is already enrolled in
                var enrolledCourseIds = await _db.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                // Get all courses not enrolled in
                var availableCourses = await _db.Courses
                    .Where(c => !enrolledCourseIds.Contains(c.CourseId))
                    .Include(c => c.Teacher)
                    .Include(c => c.Enrollments)
                    .Select(c => new AvailableCourseDto
                    {
                        CourseId = c.CourseId,
                        CourseCode = c.CourseCode,
                        CourseName = c.CourseName,
                        TeacherName = c.Teacher.FullName,
                        CurrentEnrollments = c.Enrollments.Count()
                    })
                    .OrderBy(c => c.CourseCode)
                    .ToListAsync();

                return availableCourses;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving available courses: {ex.Message}", ex);
            }
        }

        public async Task<bool> EnrollInCourseAsync(int studentId, int courseId)
        {
            // Use the execution strategy to handle retries and transactions properly
            var strategy = _db.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                
                try
                {
                    // Enhanced logging for debugging
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Starting enrollment process for Student ID: {studentId}, Course ID: {courseId}");

                    // Input validation
                    if (studentId <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Invalid student ID: {studentId}");
                        throw new ArgumentException("Invalid student ID.", nameof(studentId));
                    }

                    if (courseId <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Invalid course ID: {courseId}");
                        throw new ArgumentException("Invalid course ID.", nameof(courseId));
                    }

                    // Verify student exists and has correct role (Role = 2 for students)
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Verifying student exists...");
                    var student = await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == studentId && u.Role == 2);
                    
                    if (student == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Student not found or not a student. ID: {studentId}");
                        throw new InvalidOperationException($"Student with ID {studentId} not found or user is not a student.");
                    }

                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Student found: {student.FullName} ({student.Email})");

                    // Verify course exists and is active
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Verifying course exists...");
                    var course = await _db.Courses
                        .AsNoTracking()
                        .Include(c => c.Teacher)
                        .FirstOrDefaultAsync(c => c.CourseId == courseId);
                    
                    if (course == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Course not found. ID: {courseId}");
                        throw new InvalidOperationException($"Course with ID {courseId} not found.");
                    }

                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Course found: {course.CourseCode} - {course.CourseName}, Teacher: {course.Teacher?.FullName ?? "No Teacher"}");

                    // Additional validation: Check if teacher is active (Role = 1)
                    if (course.Teacher == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Course has no assigned teacher. Course ID: {courseId}");
                        throw new InvalidOperationException("Cannot enroll in course - no teacher assigned.");
                    }

                    if (course.Teacher.Role != 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Course teacher is not active. Teacher Role: {course.Teacher.Role}");
                        throw new InvalidOperationException("Cannot enroll in course - assigned teacher is not active.");
                    }

                    // Check if student is already enrolled in this course
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Checking for existing enrollment...");
                    var existingEnrollment = await _db.Enrollments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
                    
                    if (existingEnrollment != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Student already enrolled in course. Enrollment ID: {existingEnrollment.EnrollmentId}");
                        throw new InvalidOperationException($"Student is already enrolled in course '{course.CourseCode} - {course.CourseName}'.");
                    }

                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] No existing enrollment found. Proceeding with enrollment...");

                    // Create new enrollment record
                    var enrollment = new Enrollment
                    {
                        StudentId = studentId,
                        CourseId = courseId,
                        EnrollmentDate = DateTime.UtcNow
                    };

                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Created enrollment object for Student {studentId} in Course {courseId}");

                    // Add enrollment to database
                    await _db.Enrollments.AddAsync(enrollment);
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Enrollment added to context");
                    
                    // Save changes within the transaction
                    var rowsAffected = await _db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] SaveChanges completed. Rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] No rows affected during SaveChanges");
                        throw new InvalidOperationException("Failed to create enrollment record - no rows affected.");
                    }

                    // Commit the transaction if everything succeeded
                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Transaction committed successfully");

                    // Log successful enrollment for audit purposes
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] SUCCESS: Student {student.FullName} (ID: {studentId}) successfully enrolled in course {course.CourseCode} - {course.CourseName} (ID: {courseId}) at {DateTime.UtcNow}");

                    return true;
                }
                catch (ArgumentException ex)
                {
                    // Rollback the transaction on any error
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] ArgumentException: {ex.Message}");
                    throw;
                }
                catch (InvalidOperationException ex)
                {
                    // Rollback the transaction on any error
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] InvalidOperationException: {ex.Message}");
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    // Rollback the transaction on database errors
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Database Update Exception: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Inner Exception: {ex.InnerException?.Message}");
                    
                    // Check for specific database constraint violations
                    if (ex.InnerException?.Message?.Contains("FOREIGN KEY constraint") == true)
                    {
                        throw new InvalidOperationException("Database constraint error - invalid student or course reference.");
                    }
                    else if (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
                    {
                        throw new InvalidOperationException("Student is already enrolled in this course.");
                    }
                    else
                    {
                        throw new Exception($"Database error during enrollment: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Rollback the transaction on any error
                    await transaction.RollbackAsync();
                    
                    // Log the error for debugging with full details
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Unexpected Exception Type: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Unexpected Exception Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnrollInCourse] Inner Exception: {ex.InnerException.Message}");
                    }
                    
                    // Re-throw with more context
                    throw new Exception($"Unexpected error during enrollment: {ex.Message} (Type: {ex.GetType().Name})", ex);
                }
            });
        }

        public async Task<List<StudentAttendanceHistoryDto>> GetAttendanceHistoryAsync(int studentId)
        {
            try
            {
                var attendanceHistory = await _db.Attendances
                    .Where(a => a.Enrollment.StudentId == studentId)
                    .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Course)
                    .Include(a => a.MarkedByTeacher)
                    .OrderByDescending(a => a.Date)
                    .Select(a => new StudentAttendanceHistoryDto
                    {
                        AttendanceId = a.AttendanceId,
                        CourseCode = a.Enrollment.Course.CourseCode,
                        CourseName = a.Enrollment.Course.CourseName,
                        Date = a.Date,
                        Status = a.Status,
                        MarkedByTeacher = a.MarkedByTeacher.FullName
                    })
                    .ToListAsync();

                return attendanceHistory;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving attendance history: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetOverallAttendancePercentageAsync(int studentId)
        {
            try
            {
                var attendanceStats = await _db.Attendances
                    .Where(a => a.Enrollment.StudentId == studentId)
                    .GroupBy(a => 1)
                    .Select(g => new
                    {
                        TotalClasses = g.Count(),
                        AttendedClasses = g.Count(a => a.Status == "Present" || a.Status == "Late")
                    })
                    .FirstOrDefaultAsync();

                if (attendanceStats == null || attendanceStats.TotalClasses == 0)
                {
                    return 0;
                }

                return Math.Round((decimal)attendanceStats.AttendedClasses / attendanceStats.TotalClasses * 100, 2);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating attendance percentage: {ex.Message}", ex);
            }
        }
    }
}