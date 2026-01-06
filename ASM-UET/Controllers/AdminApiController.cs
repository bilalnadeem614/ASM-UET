using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ASM_UET.Models;
using ASM_UET.Services;

namespace ASM_UET.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "0")]
    public class AdminApiController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminApiController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ==================== DASHBOARD ENDPOINTS ====================

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve dashboard statistics", details = ex.Message });
            }
        }

        // ==================== COURSE ENDPOINTS ====================

        [HttpGet("courses")]
        public async Task<IActionResult> GetAllCourses()
        {
            try
            {
                var courses = await _adminService.GetAllCoursesAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve courses", details = ex.Message });
            }
        }

        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var course = await _adminService.CreateCourseAsync(dto);
                return CreatedAtAction(nameof(GetAllCourses), new { id = course.CourseId }, course);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to create course", details = ex.Message });
            }
        }

        [HttpPut("courses/{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != dto.CourseId)
            {
                return BadRequest(new { error = "Course ID in URL does not match the ID in request body" });
            }

            try
            {
                var course = await _adminService.UpdateCourseAsync(dto);
                return Ok(course);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                return BadRequest(new { error = "Failed to update course", details = ex.Message });
            }
        }

        [HttpDelete("courses/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var result = await _adminService.DeleteCourseAsync(id);
                if (result)
                {
                    return Ok(new { message = $"Course with ID {id} deleted successfully" });
                }
                return BadRequest(new { error = "Failed to delete course" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                if (ex.Message.Contains("enrollments"))
                {
                    return Conflict(new { error = ex.Message });
                }
                return BadRequest(new { error = "Failed to delete course", details = ex.Message });
            }
        }

        [HttpGet("teachers")]
        public async Task<IActionResult> GetAllTeachers()
        {
            try
            {
                var teachers = await _adminService.GetAllTeachersAsync();
                return Ok(teachers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve teachers", details = ex.Message });
            }
        }

        // ==================== USER MANAGEMENT ENDPOINTS ====================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int? roleFilter = null)
        {
            try
            {
                var users = await _adminService.GetAllUsersAsync(roleFilter);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve users", details = ex.Message });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                return StatusCode(500, new { error = "Failed to retrieve user details", details = ex.Message });
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _adminService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                {
                    return Conflict(new { error = ex.Message });
                }
                return BadRequest(new { error = "Failed to create user", details = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != dto.UserId)
            {
                return BadRequest(new { error = "User ID in URL does not match the ID in request body" });
            }

            try
            {
                var user = await _adminService.UpdateUserAsync(dto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                if (ex.Message.Contains("already exists"))
                {
                    return Conflict(new { error = ex.Message });
                }
                return BadRequest(new { error = "Failed to update user", details = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(id);
                if (result)
                {
                    return Ok(new { message = $"User with ID {id} deleted successfully" });
                }
                return BadRequest(new { error = "Failed to delete user" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                if (ex.Message.Contains("Cannot delete"))
                {
                    return Conflict(new { error = ex.Message });
                }
                return BadRequest(new { error = "Failed to delete user", details = ex.Message });
            }
        }

        [HttpPost("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var result = await _adminService.ToggleUserStatusAsync(id);
                if (result)
                {
                    return Ok(new { message = $"User status toggled successfully for user ID {id}" });
                }
                return BadRequest(new { error = "Failed to toggle user status" });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                return BadRequest(new { error = "Failed to toggle user status", details = ex.Message });
            }
        }

        // ==================== REPORTS ENDPOINTS ====================

        [HttpGet("reports/attendance")]
        public async Task<IActionResult> GetAttendanceReport(
            [FromQuery] int? courseId = null,
            [FromQuery] int? studentId = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                var filter = new AttendanceReportFilterDto
                {
                    CourseId = courseId,
                    StudentId = studentId
                };

                // Parse date parameters
                if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStartDate))
                {
                    filter.StartDate = parsedStartDate;
                }

                if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEndDate))
                {
                    filter.EndDate = parsedEndDate;
                }

                var report = await _adminService.GetAttendanceReportAsync(filter);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate attendance report", details = ex.Message });
            }
        }

        [HttpGet("reports/course-enrollment")]
        public async Task<IActionResult> GetCourseEnrollmentReport()
        {
            try
            {
                var report = await _adminService.GetCourseEnrollmentReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate course enrollment report", details = ex.Message });
            }
        }

        [HttpGet("reports/student-performance")]
        public async Task<IActionResult> GetStudentPerformanceReport(
            [FromQuery] int? studentId = null,
            [FromQuery] int? courseId = null)
        {
            try
            {
                var report = await _adminService.GetStudentPerformanceReportAsync(studentId, courseId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate student performance report", details = ex.Message });
            }
        }

        // ==================== CSV EXPORT ENDPOINTS ====================

        /// <summary>
        /// Export attendance report as CSV for CCP reporting requirements
        /// </summary>
        [HttpGet("reports/attendance/export-csv")]
        public async Task<IActionResult> ExportAttendanceReportCsv(
            [FromQuery] int? courseId = null,
            [FromQuery] int? studentId = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                var filter = new AttendanceReportFilterDto
                {
                    CourseId = courseId,
                    StudentId = studentId
                };

                // Parse date parameters
                if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStartDate))
                {
                    filter.StartDate = parsedStartDate;
                }

                if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEndDate))
                {
                    filter.EndDate = parsedEndDate;
                }

                var csvContent = await _adminService.ExportAttendanceReportToCsvAsync(filter);
                
                // Generate filename with timestamp
                var fileName = $"AttendanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                // Return CSV file
                return File(
                    System.Text.Encoding.UTF8.GetBytes(csvContent),
                    "text/csv",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export attendance report", details = ex.Message });
            }
        }
    }
}
