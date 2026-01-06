using ASM_UET.Models;

namespace ASM_UET.Services
{
    public interface ITeacherService
    {
        /// <summary>
        /// Gets all courses assigned to a specific teacher
        /// </summary>
        /// <param name="teacherId">The ID of the teacher</param>
        /// <returns>List of courses assigned to the teacher</returns>
        Task<List<TeacherCourseDto>> GetAssignedCoursesAsync(int teacherId);

        /// <summary>
        /// Gets all students enrolled in a specific course
        /// </summary>
        /// <param name="courseId">The ID of the course</param>
        /// <returns>List of students enrolled in the course</returns>
        Task<List<EnrolledStudentDto>> GetEnrolledStudentsAsync(int courseId);

        /// <summary>
        /// Submits attendance for multiple students in a course for a specific date
        /// </summary>
        /// <param name="attendance">Attendance submission data</param>
        /// <returns>True if submission was successful, false otherwise</returns>
        Task<bool> SubmitAttendanceAsync(AttendanceSubmissionDto attendance);

        /// <summary>
        /// Gets comprehensive statistics for a teacher including courses, students, and attendance data
        /// </summary>
        /// <param name="teacherId">The ID of the teacher</param>
        /// <returns>Teacher statistics</returns>
        Task<TeacherStatsDto> GetTeacherStatsAsync(int teacherId);
    }
}