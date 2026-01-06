using ASM_UET.Models;

namespace ASM_UET.Services
{
    public interface IStudentService
    {
        /// <summary>
        /// Gets dashboard statistics for a specific student including attendance summary
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Student dashboard statistics</returns>
        Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId);

        /// <summary>
        /// Gets current course enrollments for a student with attendance percentages
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>List of student's enrolled courses</returns>
        Task<List<StudentCourseDto>> GetStudentCoursesAsync(int studentId);

        /// <summary>
        /// Gets all available courses that a student can register for (not already enrolled)
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>List of available courses for registration</returns>
        Task<List<AvailableCourseDto>> GetAvailableCoursesAsync(int studentId);

        /// <summary>
        /// Enrolls a student in a course
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <param name="courseId">The ID of the course</param>
        /// <returns>Success status</returns>
        Task<bool> EnrollInCourseAsync(int studentId, int courseId);

        /// <summary>
        /// Gets detailed attendance history for a student across all courses
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Detailed attendance records</returns>
        Task<List<StudentAttendanceHistoryDto>> GetAttendanceHistoryAsync(int studentId);

        /// <summary>
        /// Gets overall attendance percentage across all courses for a student
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Overall attendance percentage</returns>
        Task<decimal> GetOverallAttendancePercentageAsync(int studentId);
    }

    // ==================== STUDENT DTOs ====================

    public class StudentDashboardDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int TotalEnrollments { get; set; }
        public decimal OverallAttendancePercentage { get; set; }
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public List<StudentCourseDto> RecentCourses { get; set; } = new List<StudentCourseDto>();
        public List<RecentAttendanceRecordDto> RecentAttendance { get; set; } = new List<RecentAttendanceRecordDto>();
    }

    public class StudentCourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public string TeacherName { get; set; } = null!;
        public DateTime EnrollmentDate { get; set; }
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string AttendanceStatus => AttendancePercentage switch
        {
            >= 75 => "Good",
            >= 65 => "Warning",
            >= 50 => "Poor",
            _ => "Critical"
        };
    }

    public class AvailableCourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public string TeacherName { get; set; } = null!;
        public int CurrentEnrollments { get; set; }
    }

    public class StudentAttendanceHistoryDto
    {
        public int AttendanceId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string Status { get; set; } = null!;
        public string MarkedByTeacher { get; set; } = null!;
    }

    public class RecentAttendanceRecordDto
    {
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string Status { get; set; } = null!;
    }
}