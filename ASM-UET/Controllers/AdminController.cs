using Microsoft.AspNetCore.Mvc;
using ASM_UET.Services;
using ASM_UET.Models;
using ASM_UET.Attributes;
using System.Net.Http.Headers;

namespace ASM_UET.Controllers
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(IAdminService adminService, IHttpClientFactory httpClientFactory)
        {
            _adminService = adminService;
            _httpClientFactory = httpClientFactory;
        }

        private string? GetAuthToken()
        {
            return Request.Cookies["ASM_TOKEN"];
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = GetAuthToken();
            
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();
                ViewData["Title"] = "Dashboard";
                return View(stats);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load dashboard: {ex.Message}";
                return View(new DashboardStatsDto());
            }
        }

        [HttpGet]
        public IActionResult Courses()
        {
            ViewData["Title"] = "Course Management";
            return View();
        }

        // ==================== USER MANAGEMENT ACTIONS ====================

        [HttpGet]
        public IActionResult Users(int? roleFilter)
        {
            ViewData["Title"] = "User Management";
            ViewData["RoleFilter"] = roleFilter;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(int? roleFilter)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var endpoint = roleFilter.HasValue 
                    ? $"/api/admin/users?roleFilter={roleFilter}" 
                    : "/api/admin/users";
                
                var response = await client.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
                    return Json(new { success = true, data = users });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to fetch users", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync($"/api/admin/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var userDetails = await response.Content.ReadFromJsonAsync<UserDetailsDto>();
                    return Json(new { success = true, data = userDetails });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Json(new { success = false, error = "User not found" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to fetch user details", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, error = "Invalid user data", modelState = ModelState });
                }

                using var client = CreateAuthorizedClient();
                var response = await client.PostAsJsonAsync("/api/admin/users", userDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<UserDto>();
                    TempData["Success"] = "User created successfully!";
                    return Json(new { success = true, data = user });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "User with this email already exists", details = errorContent });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to create user", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, error = "Invalid user data", modelState = ModelState });
                }

                using var client = CreateAuthorizedClient();
                var response = await client.PutAsJsonAsync($"/api/admin/users/{userDto.UserId}", userDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<UserDto>();
                    TempData["Success"] = "User updated successfully!";
                    return Json(new { success = true, data = user });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Json(new { success = false, error = "User not found" });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Email already exists", details = errorContent });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to update user", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.DeleteAsync($"/api/admin/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "User deleted successfully!";
                    return Json(new { success = true });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Json(new { success = false, error = "User not found" });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Cannot delete user with dependencies", details = errorContent });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to delete user", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ==================== OTHER ACTIONS ====================

        [HttpGet]
        public IActionResult Settings()
        {
            ViewData["Title"] = "Settings";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync("/api/admin/dashboard/stats");
                
                if (response.IsSuccessStatusCode)
                {
                    var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
                    return Json(new { success = true, data = stats });
                }
                else
                {
                    return Json(new { success = false, error = "Failed to fetch dashboard stats" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync("/api/admin/courses");
                
                if (response.IsSuccessStatusCode)
                {
                    var courses = await response.Content.ReadFromJsonAsync<List<CourseDto>>();
                    return Json(new { success = true, data = courses });
                }
                else
                {
                    return Json(new { success = false, error = "Failed to fetch courses" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeachers()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync("/api/admin/teachers");
                
                if (response.IsSuccessStatusCode)
                {
                    var teachers = await response.Content.ReadFromJsonAsync<List<TeacherDropdownDto>>();
                    return Json(new { success = true, data = teachers });
                }
                else
                {
                    return Json(new { success = false, error = "Failed to fetch teachers" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto courseDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, error = "Invalid course data" });
                }

                using var client = CreateAuthorizedClient();
                var response = await client.PostAsJsonAsync("/api/admin/courses", courseDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var course = await response.Content.ReadFromJsonAsync<CourseDto>();
                    TempData["Success"] = "Course created successfully!";
                    return Json(new { success = true, data = course });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCourse([FromBody] UpdateCourseDto courseDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, error = "Invalid course data" });
                }

                using var client = CreateAuthorizedClient();
                var response = await client.PutAsJsonAsync($"/api/admin/courses/{courseDto.CourseId}", courseDto);
                
                if (response.IsSuccessStatusCode)
                {
                    var course = await response.Content.ReadFromJsonAsync<CourseDto>();
                    TempData["Success"] = "Course updated successfully!";
                    return Json(new { success = true, data = course });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.DeleteAsync($"/api/admin/courses/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Course deleted successfully!";
                    return Json(new { success = true });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ==================== REPORTS ACTIONS ====================

        [HttpGet]
        public IActionResult Reports()
        {
            ViewData["Title"] = "Reports";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(
            int? courseId = null,
            int? studentId = null,
            string? startDate = null,
            string? endDate = null)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var queryParams = new List<string>();

                if (courseId.HasValue)
                    queryParams.Add($"courseId={courseId.Value}");
                
                if (studentId.HasValue)
                    queryParams.Add($"studentId={studentId.Value}");
                
                if (!string.IsNullOrEmpty(startDate))
                    queryParams.Add($"startDate={startDate}");
                
                if (!string.IsNullOrEmpty(endDate))
                    queryParams.Add($"endDate={endDate}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await client.GetAsync($"/api/admin/reports/attendance{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var report = await response.Content.ReadFromJsonAsync<List<AttendanceReportDto>>();
                    return Json(new { success = true, data = report });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to generate attendance report", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseEnrollmentReport()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync("/api/admin/reports/course-enrollment");
                
                if (response.IsSuccessStatusCode)
                {
                    var report = await response.Content.ReadFromJsonAsync<CourseEnrollmentReportDto>();
                    return Json(new { success = true, data = report });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to generate course enrollment report", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentPerformanceReport(int? studentId = null, int? courseId = null)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var queryParams = new List<string>();

                if (studentId.HasValue)
                    queryParams.Add($"studentId={studentId.Value}");
                
                if (courseId.HasValue)
                    queryParams.Add($"courseId={courseId.Value}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await client.GetAsync($"/api/admin/reports/student-performance{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var report = await response.Content.ReadFromJsonAsync<StudentPerformanceReportDto>();
                    return Json(new { success = true, data = report });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to generate student performance report", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesForDropdown()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync("/api/admin/courses");
                
                if (response.IsSuccessStatusCode)
                {
                    var courses = await response.Content.ReadFromJsonAsync<List<CourseDto>>();
                    return Json(new { success = true, data = courses });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to fetch courses", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentsForDropdown()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync("/api/admin/users?roleFilter=2");
                
                if (response.IsSuccessStatusCode)
                {
                    var students = await response.Content.ReadFromJsonAsync<List<UserDto>>();
                    return Json(new { success = true, data = students });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, error = "Failed to fetch students", details = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Keep existing method for backwards compatibility
        [HttpGet]
        public async Task<IActionResult> GetUsersData(int? roleFilter)
        {
            return await GetUsers(roleFilter);
        }

        // ==================== CSV EXPORT METHODS ====================

        /// <summary>
        /// Export attendance report as CSV file for CCP reporting requirements
        /// Generates a comprehensive attendance report in CSV format with audit trail information
        /// </summary>
        /// <param name="courseId">Optional filter by specific course ID</param>
        /// <param name="studentId">Optional filter by specific student ID</param>
        /// <param name="startDate">Optional start date filter (YYYY-MM-DD format)</param>
        /// <param name="endDate">Optional end date filter (YYYY-MM-DD format)</param>
        /// <returns>FileContentResult containing CSV file with MIME type text/csv</returns>
        [HttpGet]
        public async Task<IActionResult> ExportAttendanceReportCsv(
            int? courseId = null,
            int? studentId = null,
            string? startDate = null,
            string? endDate = null)
        {
            try
            {
                // Create filter object with validation
                var filter = new AttendanceReportFilterDto
                {
                    CourseId = courseId,
                    StudentId = studentId
                };

                // Parse and validate date parameters
                if (!string.IsNullOrEmpty(startDate))
                {
                    if (!DateOnly.TryParse(startDate, out var parsedStartDate))
                    {
                        TempData["Error"] = "Invalid start date format. Please use YYYY-MM-DD format.";
                        return RedirectToAction("Reports");
                    }
                    filter.StartDate = parsedStartDate;
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    if (!DateOnly.TryParse(endDate, out var parsedEndDate))
                    {
                        TempData["Error"] = "Invalid end date format. Please use YYYY-MM-DD format.";
                        return RedirectToAction("Reports");
                    }
                    filter.EndDate = parsedEndDate;
                }

                // Validate date range
                if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
                {
                    TempData["Error"] = "Start date cannot be later than end date.";
                    return RedirectToAction("Reports");
                }

                // Get CSV content from service using StringBuilder
                var csvContent = await _adminService.ExportAttendanceReportToCsvAsync(filter);
                
                // Generate descriptive filename with timestamp for CCP compliance
                var filterSuffix = "";
                if (courseId.HasValue || studentId.HasValue || !string.IsNullOrEmpty(startDate) || !string.IsNullOrEmpty(endDate))
                {
                    filterSuffix = "_Filtered";
                }
                var fileName = $"ASM_UET_AttendanceReport{filterSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                // Return as FileContentResult with proper MIME type for CCP requirements
                return File(
                    System.Text.Encoding.UTF8.GetBytes(csvContent),
                    "text/csv",
                    fileName
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to export attendance report: {ex.Message}";
                return RedirectToAction("Reports");
            }
        }
    }
}
