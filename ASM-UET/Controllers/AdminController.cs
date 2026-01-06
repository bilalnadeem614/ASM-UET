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

        [HttpGet]
        public async Task<IActionResult> Users(int? roleFilter)
        {
            try
            {
                // Fetch users via API
                using var client = CreateAuthorizedClient();
                var endpoint = roleFilter.HasValue 
                    ? $"/api/admin/users?roleFilter={roleFilter}" 
                    : "/api/admin/users";
                
                var response = await client.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
                    ViewData["Title"] = "User Management";
                    ViewData["RoleFilter"] = roleFilter;
                    return View(users ?? new List<UserDto>());
                }
                else
                {
                    TempData["Error"] = $"Failed to load users: {response.StatusCode}";
                    return View(new List<UserDto>());
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading users: {ex.Message}";
                return View(new List<UserDto>());
            }
        }

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

        [HttpGet]
        public async Task<IActionResult> GetUsersData(int? roleFilter)
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
                    return Json(new { success = false, error = "Failed to fetch users" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
