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

        // ==================== USER MANAGEMENT METHODS ====================

        public async Task<UserDetailsDto> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    throw new Exception($"User with ID {userId} not found.");
                }

                // Get counts based on role
                var courseCount = 0;
                var enrollmentCount = 0;

                if (user.Role == 1) // Teacher
                {
                    courseCount = await _db.Courses.CountAsync(c => c.TeacherId == userId);
                }
                else if (user.Role == 2) // Student
                {
                    enrollmentCount = await _db.Enrollments.CountAsync(e => e.StudentId == userId);
                }

                return new UserDetailsDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    RoleName = user.Role == 0 ? "Admin" : user.Role == 1 ? "Teacher" : "Student",
                    CreatedAt = user.CreatedAt,
                    CourseCount = courseCount,
                    EnrollmentCount = enrollmentCount,
                    IsActive = true // Default for now, can be extended later
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user details: {ex.Message}", ex);
            }
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            try
            {
                // Check for duplicate email
                var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                {
                    throw new Exception($"User with email '{dto.Email}' already exists.");
                }

                // Validate role
                if (dto.Role < 0 || dto.Role > 2)
                {
                    throw new Exception("Invalid role. Role must be 0 (Admin), 1 (Teacher), or 2 (Student).");
                }

                // Hash password using Base64 (matching AuthService)
                var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dto.Password));

                var user = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PasswordHash = passwordHash,
                    Role = dto.Role,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                return new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    RoleName = user.Role == 0 ? "Admin" : user.Role == 1 ? "Teacher" : "Student",
                    CreatedAt = user.CreatedAt
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating user: {ex.Message}", ex);
            }
        }

        public async Task<UserDto> UpdateUserAsync(UpdateUserDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);
                if (user == null)
                {
                    throw new Exception($"User with ID {dto.UserId} not found.");
                }

                // Check for duplicate email (excluding current user)
                var duplicateUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.UserId != dto.UserId);
                if (duplicateUser != null)
                {
                    throw new Exception($"Another user with email '{dto.Email}' already exists.");
                }

                // Validate role
                if (dto.Role < 0 || dto.Role > 2)
                {
                    throw new Exception("Invalid role. Role must be 0 (Admin), 1 (Teacher), or 2 (Student).");
                }

                // Update user details (not updating password here)
                user.FullName = dto.FullName;
                user.Email = dto.Email;
                user.Role = dto.Role;

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                return new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    RoleName = user.Role == 0 ? "Admin" : user.Role == 1 ? "Teacher" : "Student",
                    CreatedAt = user.CreatedAt
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating user: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    throw new Exception($"User with ID {userId} not found.");
                }

                // Check dependencies based on user role
                if (user.Role == 1) // Teacher
                {
                    var hasCourses = await _db.Courses.AnyAsync(c => c.TeacherId == userId);
                    if (hasCourses)
                    {
                        throw new Exception("Cannot delete teacher with assigned courses. Please reassign or remove courses first.");
                    }
                }
                else if (user.Role == 2) // Student
                {
                    var hasEnrollments = await _db.Enrollments.AnyAsync(e => e.StudentId == userId);
                    if (hasEnrollments)
                    {
                        throw new Exception("Cannot delete student with active enrollments. Please remove enrollments first.");
                    }

                    // Check for attendance records through enrollments
                    var hasAttendances = await _db.Attendances
                        .AnyAsync(a => _db.Enrollments.Any(e => e.EnrollmentId == a.EnrollmentId && e.StudentId == userId));
                    if (hasAttendances)
                    {
                        throw new Exception("Cannot delete student with attendance records. Please remove attendance records first.");
                    }
                }

                _db.Users.Remove(user);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting user: {ex.Message}", ex);
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    throw new Exception($"User with ID {userId} not found.");
                }

                // Note: Since User model doesn't have IsActive field yet, 
                // this is a placeholder implementation that returns success
                // In a real implementation, you would add IsActive field to User model
                // and update it here: user.IsActive = !user.IsActive;
                
                // For now, we'll just return success to maintain API contract
                // _db.Users.Update(user);
                // await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error toggling user status: {ex.Message}", ex);
            }
        }

        // ==================== REPORTS METHODS ====================

        public async Task<List<AttendanceReportDto>> GetAttendanceReportAsync(AttendanceReportFilterDto filter)
        {
            try
            {
                var query = _db.Attendances
                    .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Student)
                    .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Course)
                    .AsQueryable();

                // Apply filters
                if (filter.CourseId.HasValue)
                {
                    query = query.Where(a => a.Enrollment.CourseId == filter.CourseId.Value);
                }

                if (filter.StudentId.HasValue)
                {
                    query = query.Where(a => a.Enrollment.StudentId == filter.StudentId.Value);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(a => a.Date >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(a => a.Date <= filter.EndDate.Value);
                }

                // Group by Student and Course to calculate attendance statistics
                var attendanceData = await query
                    .GroupBy(a => new { a.Enrollment.StudentId, a.Enrollment.CourseId })
                    .Select(g => new
                    {
                        StudentId = g.Key.StudentId,
                        CourseId = g.Key.CourseId,
                        StudentName = g.First().Enrollment.Student.FullName,
                        CourseName = g.First().Enrollment.Course.CourseName,
                        TotalClasses = g.Count(),
                        PresentCount = g.Count(a => a.Status == "Present"),
                        AbsentCount = g.Count(a => a.Status == "Absent"),
                        LateCount = g.Count(a => a.Status == "Late")
                    })
                    .ToListAsync();

                var result = attendanceData.Select(data => new AttendanceReportDto
                {
                    CourseName = data.CourseName,
                    StudentName = data.StudentName,
                    TotalClasses = data.TotalClasses,
                    PresentCount = data.PresentCount,
                    AbsentCount = data.AbsentCount,
                    LateCount = data.LateCount,
                    AttendancePercentage = data.TotalClasses > 0 
                        ? Math.Round((decimal)(data.PresentCount + data.LateCount) / data.TotalClasses * 100, 2)
                        : 0
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating attendance report: {ex.Message}", ex);
            }
        }

        public async Task<CourseEnrollmentReportDto> GetCourseEnrollmentReportAsync()
        {
            try
            {
                var courseSummaries = await _db.Courses
                    .Include(c => c.Teacher)
                    .Select(c => new
                    {
                        c.CourseCode,
                        c.CourseName,
                        TeacherName = c.Teacher.FullName,
                        EnrollmentCount = c.Enrollments.Count(),
                        // Calculate average attendance for this course
                        AverageAttendance = c.Enrollments
                            .SelectMany(e => e.Attendances)
                            .GroupBy(a => a.EnrollmentId)
                            .Select(g => new
                            {
                                TotalClasses = g.Count(),
                                PresentClasses = g.Count(a => a.Status == "Present" || a.Status == "Late")
                            })
                            .Average(x => x.TotalClasses > 0 ? (decimal)x.PresentClasses / x.TotalClasses * 100 : 0)
                    })
                    .ToListAsync();

                var courses = courseSummaries.Select(c => new CourseEnrollmentSummary
                {
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    TeacherName = c.TeacherName,
                    EnrollmentCount = c.EnrollmentCount,
                    AverageAttendance = Math.Round(c.AverageAttendance, 2)
                }).ToList();

                var totalEnrollments = courses.Sum(c => c.EnrollmentCount);
                var averageEnrollmentPerCourse = courses.Count > 0 
                    ? Math.Round((decimal)totalEnrollments / courses.Count, 2) 
                    : 0;

                return new CourseEnrollmentReportDto
                {
                    Courses = courses,
                    TotalCourses = courses.Count,
                    TotalEnrollments = totalEnrollments,
                    AverageEnrollmentPerCourse = averageEnrollmentPerCourse
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating course enrollment report: {ex.Message}", ex);
            }
        }

        public async Task<StudentPerformanceReportDto> GetStudentPerformanceReportAsync(int? studentId, int? courseId)
        {
            try
            {
                var query = _db.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .Include(e => e.Attendances)
                    .AsQueryable();

                // Apply filters
                if (studentId.HasValue)
                {
                    query = query.Where(e => e.StudentId == studentId.Value);
                }

                if (courseId.HasValue)
                {
                    query = query.Where(e => e.CourseId == courseId.Value);
                }

                var enrollmentData = await query
                    .Select(e => new
                    {
                        StudentName = e.Student.FullName,
                        CourseName = e.Course.CourseName,
                        TotalClasses = e.Attendances.Count(),
                        PresentCount = e.Attendances.Count(a => a.Status == "Present" || a.Status == "Late")
                    })
                    .ToListAsync();

                var students = enrollmentData.Select(e => new StudentPerformance
                {
                    StudentName = e.StudentName,
                    CourseName = e.CourseName,
                    TotalClasses = e.TotalClasses,
                    PresentCount = e.PresentCount,
                    AttendancePercentage = e.TotalClasses > 0 
                        ? Math.Round((decimal)e.PresentCount / e.TotalClasses * 100, 2)
                        : 0
                }).ToList();

                var overallAverageAttendance = students.Count > 0
                    ? Math.Round(students.Average(s => s.AttendancePercentage), 2)
                    : 0;

                return new StudentPerformanceReportDto
                {
                    Students = students,
                    TotalStudents = students.GroupBy(s => s.StudentName).Count(),
                    OverallAverageAttendance = overallAverageAttendance
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating student performance report: {ex.Message}", ex);
            }
        }

        // ==================== END REPORTS METHODS ===================
    }
}
