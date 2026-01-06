using System.ComponentModel.DataAnnotations;

namespace ASM_UET.Models
{
    public class DashboardStatsDto
    {
        public int TotalCourses { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public List<RecentEnrollmentDto> RecentEnrollments { get; set; } = new List<RecentEnrollmentDto>();
    }

    public class RecentEnrollmentDto
    {
        public string StudentName { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public DateTime EnrollmentDate { get; set; }
    }

    // Course DTOs
    public class CourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = null!;
    }

    public class CreateCourseDto
    {
        [Required]
        public string CourseCode { get; set; } = null!;

        [Required]
        public string CourseName { get; set; } = null!;

        [Required]
        public int TeacherId { get; set; }
    }

    public class UpdateCourseDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string CourseCode { get; set; } = null!;

        [Required]
        public string CourseName { get; set; } = null!;

        [Required]
        public int TeacherId { get; set; }
    }

    // Basic User DTOs
    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int Role { get; set; }
        public string RoleName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class TeacherDropdownDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
    }

    // User Management DTOs
    public class UserDetailsDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int Role { get; set; }
        public string RoleName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int CourseCount { get; set; } // For teachers
        public int EnrollmentCount { get; set; } // For students
        public bool IsActive { get; set; } = true; // Default to active
    }

    public class CreateUserDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; } = null!;

        [Required]
        [Range(0, 2, ErrorMessage = "Role must be 0 (Admin), 1 (Teacher), or 2 (Student)")]
        public int Role { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; } = null!;

        [Required]
        [Range(0, 2, ErrorMessage = "Role must be 0 (Admin), 1 (Teacher), or 2 (Student)")]
        public int Role { get; set; }
    }

    // ==================== REPORTS DTOs ====================

    // Filter DTO for Attendance Reports
    public class AttendanceReportFilterDto
    {
        public int? CourseId { get; set; }
        public int? StudentId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    // Attendance Report Result DTO
    public class AttendanceReportDto
    {
        public string CourseName { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public decimal AttendancePercentage { get; set; }
    }

    // Course Enrollment Report DTOs
    public class CourseEnrollmentReportDto
    {
        public List<CourseEnrollmentSummary> Courses { get; set; } = new List<CourseEnrollmentSummary>();
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal AverageEnrollmentPerCourse { get; set; }
    }

    public class CourseEnrollmentSummary
    {
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public string TeacherName { get; set; } = null!;
        public int EnrollmentCount { get; set; }
        public decimal AverageAttendance { get; set; }
    }

    // Student Performance Report DTOs
    public class StudentPerformanceReportDto
    {
        public List<StudentPerformance> Students { get; set; } = new List<StudentPerformance>();
        public int TotalStudents { get; set; }
        public decimal OverallAverageAttendance { get; set; }
    }

    public class StudentPerformance
    {
        public string StudentName { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public decimal AttendancePercentage { get; set; }
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
    }
}
