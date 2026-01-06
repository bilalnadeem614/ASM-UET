using System.ComponentModel.DataAnnotations;

namespace ASM_UET.Models
{
    // ==================== TEACHER COURSE DTOs ====================

    public class TeacherCourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public int EnrollmentCount { get; set; }
    }

    // ==================== ENROLLED STUDENT DTOs ====================

    public class EnrolledStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime EnrollmentDate { get; set; }
        public decimal AttendancePercentage { get; set; }
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
    }

    // ==================== ATTENDANCE SUBMISSION DTOs ====================

    public class AttendanceSubmissionDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [Required]
        public List<StudentAttendanceDto> StudentAttendances { get; set; } = new List<StudentAttendanceDto>();
    }

    public class StudentAttendanceDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; } = null!; // "Present", "Absent", "Late"
    }

    // ==================== TEACHER STATS DTOs ====================

    public class TeacherStatsDto
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public decimal OverallAttendanceRate { get; set; }
        public List<CourseStatsDto> CourseStats { get; set; } = new List<CourseStatsDto>();
        public List<RecentAttendanceDto> RecentAttendance { get; set; } = new List<RecentAttendanceDto>();
        
        // Computed properties for view convenience
        public decimal AverageAttendance => OverallAttendanceRate;
        public List<RecentAttendanceDto> RecentAttendances => RecentAttendance;
    }

    public class CourseStatsDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public int EnrollmentCount { get; set; }
        public int TotalClasses { get; set; }
        public decimal AverageAttendanceRate { get; set; }
    }

    public class RecentAttendanceDto
    {
        public int AttendanceId { get; set; }
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public DateOnly Date { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int TotalStudents { get; set; }
        
        // Computed property for view convenience
        public int StudentCount => TotalStudents;
    }
}