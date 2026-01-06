using Microsoft.EntityFrameworkCore;
using ASM_UET.Models;

namespace ASM_UET.Services
{
    public class AdminService : IAdminService
    {
        private readonly ASM _db;

        public AdminService(ASM db)
        {
            _db = db;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            try
            {
                var totalCourses = await _db.Courses.CountAsync();
                var totalTeachers = await _db.Users.CountAsync(u => u.Role == 1);
                var totalStudents = await _db.Users.CountAsync(u => u.Role == 2);

                var recentEnrollments = await _db.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .OrderByDescending(e => e.EnrollmentDate)
                    .Take(5)
                    .Select(e => new RecentEnrollmentDto
                    {
                        StudentName = e.Student.FullName,
                        CourseName = e.Course.CourseName,
                        EnrollmentDate = e.EnrollmentDate
                    })
                    .ToListAsync();

                return new DashboardStatsDto
                {
                    TotalCourses = totalCourses,
                    TotalTeachers = totalTeachers,
                    TotalStudents = totalStudents,
                    RecentEnrollments = recentEnrollments
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving dashboard statistics: {ex.Message}", ex);
            }
        }

        public async Task<List<CourseDto>> GetAllCoursesAsync()
        {
            try
            {
                var courses = await _db.Courses
                    .Include(c => c.Teacher)
                    .Select(c => new CourseDto
                    {
                        CourseId = c.CourseId,
                        CourseCode = c.CourseCode,
                        CourseName = c.CourseName,
                        TeacherId = c.TeacherId,
                        TeacherName = c.Teacher.FullName
                    })
                    .ToListAsync();

                return courses;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving courses: {ex.Message}", ex);
            }
        }

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            try
            {
                // Verify teacher exists and has correct role
                var teacher = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.TeacherId && u.Role == 1);
                if (teacher == null)
                {
                    throw new Exception("Invalid teacher ID or user is not a teacher.");
                }

                // Check for duplicate course code
                var existingCourse = await _db.Courses.FirstOrDefaultAsync(c => c.CourseCode == dto.CourseCode);
                if (existingCourse != null)
                {
                    throw new Exception($"Course with code '{dto.CourseCode}' already exists.");
                }

                var course = new Course
                {
                    CourseCode = dto.CourseCode,
                    CourseName = dto.CourseName,
                    TeacherId = dto.TeacherId
                };

                _db.Courses.Add(course);
                await _db.SaveChangesAsync();

                return new CourseDto
                {
                    CourseId = course.CourseId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    TeacherId = course.TeacherId,
                    TeacherName = teacher.FullName
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating course: {ex.Message}", ex);
            }
        }

        public async Task<CourseDto> UpdateCourseAsync(UpdateCourseDto dto)
        {
            try
            {
                var course = await _db.Courses.Include(c => c.Teacher).FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);
                if (course == null)
                {
                    throw new Exception($"Course with ID {dto.CourseId} not found.");
                }

                // Verify teacher exists and has correct role
                var teacher = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.TeacherId && u.Role == 1);
                if (teacher == null)
                {
                    throw new Exception("Invalid teacher ID or user is not a teacher.");
                }

                // Check for duplicate course code (excluding current course)
                var duplicateCourse = await _db.Courses.FirstOrDefaultAsync(c => c.CourseCode == dto.CourseCode && c.CourseId != dto.CourseId);
                if (duplicateCourse != null)
                {
                    throw new Exception($"Another course with code '{dto.CourseCode}' already exists.");
                }

                course.CourseCode = dto.CourseCode;
                course.CourseName = dto.CourseName;
                course.TeacherId = dto.TeacherId;

                _db.Courses.Update(course);
                await _db.SaveChangesAsync();

                return new CourseDto
                {
                    CourseId = course.CourseId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    TeacherId = course.TeacherId,
                    TeacherName = teacher.FullName
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating course: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            try
            {
                var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null)
                {
                    throw new Exception($"Course with ID {courseId} not found.");
                }

                // Check if course has any enrollments
                var hasEnrollments = await _db.Enrollments.AnyAsync(e => e.CourseId == courseId);
                if (hasEnrollments)
                {
                    throw new Exception("Cannot delete course with existing enrollments. Please remove all enrollments first.");
                }

                _db.Courses.Remove(course);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting course: {ex.Message}", ex);
            }
        }

        public async Task<List<TeacherDropdownDto>> GetAllTeachersAsync()
        {
            try
            {
                var teachers = await _db.Users
                    .Where(u => u.Role == 1)
                    .Select(u => new TeacherDropdownDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName
                    })
                    .OrderBy(t => t.FullName)
                    .ToListAsync();

                return teachers;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving teachers: {ex.Message}", ex);
            }
        }

        public async Task<List<UserDto>> GetAllUsersAsync(int? roleFilter = null)
        {
            try
            {
                var query = _db.Users.AsQueryable();

                if (roleFilter.HasValue)
                {
                    query = query.Where(u => u.Role == roleFilter.Value);
                }

                var users = await query
                    .Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email,
                        Role = u.Role,
                        RoleName = u.Role == 0 ? "Admin" : u.Role == 1 ? "Teacher" : "Student",
                        CreatedAt = u.CreatedAt
                    })
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving users: {ex.Message}", ex);
            }
        }
    }
}
