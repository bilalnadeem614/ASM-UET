using ASM_UET.Models;

namespace ASM_UET.Services
{
    public interface IAdminService
    {
        // Dashboard and existing functionality
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<CourseDto>> GetAllCoursesAsync();
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<CourseDto> UpdateCourseAsync(UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int courseId);
        Task<List<TeacherDropdownDto>> GetAllTeachersAsync();
        Task<List<UserDto>> GetAllUsersAsync(int? roleFilter = null);

        // User Management functionality
        Task<UserDetailsDto> GetUserByIdAsync(int userId);
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        Task<UserDto> UpdateUserAsync(UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ToggleUserStatusAsync(int userId);

        // Reports functionality
        Task<List<AttendanceReportDto>> GetAttendanceReportAsync(AttendanceReportFilterDto filter);
        Task<CourseEnrollmentReportDto> GetCourseEnrollmentReportAsync();
        Task<StudentPerformanceReportDto> GetStudentPerformanceReportAsync(int? studentId, int? courseId);
        
        // CSV Export functionality for CCP reporting requirements
        Task<string> ExportAttendanceReportToCsvAsync(AttendanceReportFilterDto filter);
    }
}
