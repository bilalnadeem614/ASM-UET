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
        
        /// <summary>
        /// Students with less than 75% attendance - CCP Depth of Analysis requirement
        /// </summary>
        public List<TopAbsentStudentDto> TopAbsentStudents { get; set; } = new List<TopAbsentStudentDto>();
        
        // Computed properties for view convenience
        public decimal AverageAttendance => OverallAttendanceRate;
        public List<RecentAttendanceDto> RecentAttendances => RecentAttendance;
        
        /// <summary>
        /// Count of students requiring attention due to poor attendance
        /// </summary>
        public int StudentsRequiringAttention => TopAbsentStudents.Count;
        
        /// <summary>
        /// Count of critical attendance cases (less than 50%)
        /// </summary>
        public int CriticalAttendanceCases => TopAbsentStudents.Count(s => s.AttendancePercentage < 50);
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

    // ==================== TOP ABSENT STUDENTS DTO ====================

    /// <summary>
    /// DTO for students with low attendance (less than 75%) - CCP Depth of Analysis requirement
    /// </summary>
    public class TopAbsentStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public int DaysAbsent => AbsentCount;
        public string AttendanceStatus => AttendancePercentage switch
        {
            < 50 => "Critical",
            < 65 => "Poor", 
            < 75 => "Warning",
            _ => "Good"
        };
    }
}