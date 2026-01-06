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
}
