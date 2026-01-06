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
    }
}
