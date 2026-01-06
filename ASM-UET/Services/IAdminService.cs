using ASM_UET.Models;

namespace ASM_UET.Services
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<CourseDto>> GetAllCoursesAsync();
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<CourseDto> UpdateCourseAsync(UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int courseId);
        Task<List<TeacherDropdownDto>> GetAllTeachersAsync();
        Task<List<UserDto>> GetAllUsersAsync(int? roleFilter = null);
    }
}
