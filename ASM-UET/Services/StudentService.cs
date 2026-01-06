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
            // Use a database transaction to ensure data integrity
            using var transaction = await _db.Database.BeginTransactionAsync();
            
            try
            {
                // Input validation
                if (studentId <= 0)
                {
                    throw new ArgumentException("Invalid student ID.", nameof(studentId));
                }

                if (courseId <= 0)
                {
                    throw new ArgumentException("Invalid course ID.", nameof(courseId));
                }

                // Verify student exists and has correct role (Role = 2 for students)
                var student = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == studentId && u.Role == 2);
                
                if (student == null)
                {
                    throw new InvalidOperationException($"Student with ID {studentId} not found or user is not a student.");
                }

                // Verify course exists and is active
                var course = await _db.Courses
                    .AsNoTracking()
                    .Include(c => c.Teacher)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);
                
                if (course == null)
                {
                    throw new InvalidOperationException($"Course with ID {courseId} not found.");
                }

                // Additional validation: Check if teacher is active (Role = 1)
                if (course.Teacher.Role != 1)
                {
                    throw new InvalidOperationException("Cannot enroll in course - assigned teacher is not active.");
                }

                // Check if student is already enrolled in this course
                var existingEnrollment = await _db.Enrollments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
                
                if (existingEnrollment != null)
                {
                    throw new InvalidOperationException($"Student is already enrolled in course '{course.CourseCode} - {course.CourseName}'.");
                }

                // Optional: Check enrollment capacity (if there's a limit)
                // var currentEnrollmentCount = await _db.Enrollments.CountAsync(e => e.CourseId == courseId);
                // if (currentEnrollmentCount >= course.MaxCapacity) // Assuming MaxCapacity property exists
                // {
                //     throw new InvalidOperationException("Course enrollment is full.");
                // }

                // Create new enrollment record
                var enrollment = new Enrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    EnrollmentDate = DateTime.UtcNow
                };

                // Add enrollment to database
                await _db.Enrollments.AddAsync(enrollment);
                
                // Save changes within the transaction
                var rowsAffected = await _db.SaveChangesAsync();
                
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("Failed to create enrollment record.");
                }

                // Commit the transaction if everything succeeded
                await transaction.CommitAsync();

                // Log successful enrollment for audit purposes
                System.Diagnostics.Debug.WriteLine($"Student {student.FullName} (ID: {studentId}) successfully enrolled in course {course.CourseCode} - {course.CourseName} (ID: {courseId}) at {DateTime.UtcNow}");

                return true;
            }
            catch (Exception ex)
            {
                // Rollback the transaction on any error
                await transaction.RollbackAsync();
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error enrolling student {studentId} in course {courseId}: {ex.Message}");
                
                // Re-throw with more context
                throw new Exception($"Error enrolling in course: {ex.Message}", ex);
            }
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