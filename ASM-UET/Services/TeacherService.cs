using ASM_UET.Models;
using Microsoft.EntityFrameworkCore;

namespace ASM_UET.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly ASM _context;

        public TeacherService(ASM context)
        {
            _context = context;
        }

        public async Task<List<TeacherCourseDto>> GetAssignedCoursesAsync(int teacherId)
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.TeacherId == teacherId)
                    .Select(c => new TeacherCourseDto
                    {
                        CourseId = c.CourseId,
                        CourseCode = c.CourseCode,
                        CourseName = c.CourseName,
                        EnrollmentCount = c.Enrollments.Count()
                    })
                    .OrderBy(c => c.CourseCode)
                    .ToListAsync();

                return courses;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve assigned courses for teacher {teacherId}: {ex.Message}", ex);
            }
        }

        public async Task<List<EnrolledStudentDto>> GetEnrolledStudentsAsync(int courseId)
        {
            try
            {
                // First, verify that the course exists
                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == courseId);
                if (!courseExists)
                {
                    throw new Exception($"Course with ID {courseId} not found");
                }

                var enrolledStudents = await _context.Enrollments
                    .Where(e => e.CourseId == courseId)
                    .Include(e => e.Student)
                    .Include(e => e.Attendances)
                    .Select(e => new EnrolledStudentDto
                    {
                        StudentId = e.StudentId,
                        StudentName = e.Student.FullName,
                        Email = e.Student.Email,
                        EnrollmentDate = e.EnrollmentDate,
                        TotalClasses = e.Attendances.Count(),
                        PresentCount = e.Attendances.Count(a => a.Status == "Present"),
                        AttendancePercentage = e.Attendances.Any() 
                            ? Math.Round((decimal)e.Attendances.Count(a => a.Status == "Present") / e.Attendances.Count() * 100, 2)
                            : 0
                    })
                    .OrderBy(s => s.StudentName)
                    .ToListAsync();

                return enrolledStudents;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve enrolled students for course {courseId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> SubmitAttendanceAsync(AttendanceSubmissionDto attendance)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify teacher exists and course is assigned to teacher
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == attendance.CourseId && c.TeacherId == attendance.TeacherId);

                if (course == null)
                {
                    throw new Exception($"Course {attendance.CourseId} not found or not assigned to teacher {attendance.TeacherId}");
                }

                // Check if attendance already exists for this date
                var existingAttendance = await _context.Attendances
                    .Include(a => a.Enrollment)
                    .Where(a => a.Enrollment.CourseId == attendance.CourseId && a.Date == attendance.Date)
                    .ToListAsync();

                if (existingAttendance.Any())
                {
                    throw new Exception($"Attendance for course {attendance.CourseId} on {attendance.Date} has already been submitted");
                }

                // Verify all students are enrolled in the course
                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.CourseId == attendance.CourseId)
                    .Select(e => e.StudentId)
                    .ToHashSetAsync();

                var invalidStudentIds = attendance.StudentAttendances
                    .Select(sa => sa.StudentId)
                    .Where(sid => !enrolledStudentIds.Contains(sid))
                    .ToList();

                if (invalidStudentIds.Any())
                {
                    throw new Exception($"Students with IDs [{string.Join(", ", invalidStudentIds)}] are not enrolled in course {attendance.CourseId}");
                }

                // Create attendance records
                var attendanceRecords = new List<Attendance>();

                foreach (var studentAttendance in attendance.StudentAttendances)
                {
                    // Get enrollment ID
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.CourseId == attendance.CourseId && e.StudentId == studentAttendance.StudentId);

                    if (enrollment == null)
                    {
                        throw new Exception($"Enrollment not found for student {studentAttendance.StudentId} in course {attendance.CourseId}");
                    }

                    // Validate status
                    if (!IsValidAttendanceStatus(studentAttendance.Status))
                    {
                        throw new Exception($"Invalid attendance status '{studentAttendance.Status}' for student {studentAttendance.StudentId}. Valid statuses are: Present, Absent, Late");
                    }

                    var attendanceRecord = new Attendance
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        Date = attendance.Date,
                        Status = studentAttendance.Status,
                        MarkedByTeacherId = attendance.TeacherId
                    };

                    attendanceRecords.Add(attendanceRecord);
                }

                // Add all attendance records
                await _context.Attendances.AddRangeAsync(attendanceRecords);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Failed to submit attendance: {ex.Message}", ex);
            }
        }

        public async Task<TeacherStatsDto> GetTeacherStatsAsync(int teacherId)
        {
            try
            {
                // Verify teacher exists
                var teacher = await _context.Users.FirstOrDefaultAsync(u => u.UserId == teacherId && u.Role == 1);
                if (teacher == null)
                {
                    throw new Exception($"Teacher with ID {teacherId} not found");
                }

                // Get teacher's courses
                var courses = await _context.Courses
                    .Where(c => c.TeacherId == teacherId)
                    .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Attendances)
                    .ToListAsync();

                var totalStudents = courses.SelectMany(c => c.Enrollments).Count();
                var totalClasses = courses.SelectMany(c => c.Enrollments).SelectMany(e => e.Attendances).Count();
                var totalPresent = courses
                    .SelectMany(c => c.Enrollments)
                    .SelectMany(e => e.Attendances)
                    .Count(a => a.Status == "Present");

                var overallAttendanceRate = totalClasses > 0 
                    ? Math.Round((decimal)totalPresent / totalClasses * 100, 2) 
                    : 0;

                // Generate course statistics
                var courseStats = courses.Select(course =>
                {
                    var courseAttendances = course.Enrollments.SelectMany(e => e.Attendances).ToList();
                    var coursePresentCount = courseAttendances.Count(a => a.Status == "Present");
                    var courseTotalClasses = courseAttendances.Count();

                    return new CourseStatsDto
                    {
                        CourseId = course.CourseId,
                        CourseCode = course.CourseCode,
                        CourseName = course.CourseName,
                        EnrollmentCount = course.Enrollments.Count(),
                        TotalClasses = courseTotalClasses,
                        AverageAttendanceRate = courseTotalClasses > 0 
                            ? Math.Round((decimal)coursePresentCount / courseTotalClasses * 100, 2) 
                            : 0
                    };
                }).OrderBy(cs => cs.CourseCode).ToList();

                // Get recent attendance (last 10 records)
                var recentAttendance = await _context.Attendances
                    .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Course)
                    .Where(a => a.MarkedByTeacherId == teacherId)
                    .GroupBy(a => new { a.Enrollment.CourseId, a.Date })
                    .Select(g => new RecentAttendanceDto
                    {
                        AttendanceId = g.First().AttendanceId,
                        CourseCode = g.First().Enrollment.Course.CourseCode,
                        CourseName = g.First().Enrollment.Course.CourseName,
                        Date = g.Key.Date,
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AbsentCount = g.Count(a => a.Status == "Absent"),
                        LateCount = g.Count(a => a.Status == "Late"),
                        TotalStudents = g.Count()
                    })
                    .OrderByDescending(ra => ra.Date)
                    .Take(10)
                    .ToListAsync();

                return new TeacherStatsDto
                {
                    TotalCourses = courses.Count,
                    TotalStudents = totalStudents,
                    TotalClasses = totalClasses,
                    OverallAttendanceRate = overallAttendanceRate,
                    CourseStats = courseStats,
                    RecentAttendance = recentAttendance
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve teacher statistics for teacher {teacherId}: {ex.Message}", ex);
            }
        }

        private static bool IsValidAttendanceStatus(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "present" => true,
                "absent" => true,
                "late" => true,
                _ => false
            };
        }
    }
}