using Microsoft.AspNetCore.Mvc;
using ASM_UET.Services;
using ASM_UET.Models;
using ASM_UET.Attributes;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ASM_UET.Controllers
{
    [TeacherOnly]
    public class TeacherController : Controller
    {
        private readonly ITeacherService _teacherService;
        private readonly IHttpClientFactory _httpClientFactory;

        public TeacherController(ITeacherService teacherService, IHttpClientFactory httpClientFactory)
        {
            _teacherService = teacherService;
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

        /// <summary>
        /// Gets the current teacher's user ID from JWT claims
        /// </summary>
        private int GetCurrentTeacherId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var teacherId))
            {
                return teacherId;
            }
            throw new UnauthorizedAccessException("Unable to identify current teacher");
        }

        /// <summary>
        /// Debug action to inspect JWT claims - Remove in production
        /// </summary>
        [HttpGet]
        public IActionResult Debug()
        {
            var claimsList = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList();
            
            ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            ViewBag.UserName = User?.Identity?.Name ?? "N/A";
            ViewBag.AuthenticationType = User?.Identity?.AuthenticationType ?? "N/A";
            ViewBag.Claims = claimsList;
            ViewBag.HasCookie = Request.Cookies.ContainsKey("ASM_TOKEN");
            ViewBag.CookieLength = Request.Cookies["ASM_TOKEN"]?.Length ?? 0;

            // Decode token if exists
            string tokenInfo = "No token found";
            if (!string.IsNullOrEmpty(Request.Cookies["ASM_TOKEN"]))
            {
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var token = handler.ReadToken(Request.Cookies["ASM_TOKEN"]) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                    if (token != null)
                    {
                        tokenInfo = string.Join("\n", token.Claims.Select(c => $"{c.Type}: {c.Value}"));
                    }
                }
                catch (Exception ex)
                {
                    tokenInfo = $"Error decoding token: {ex.Message}";
                }
            }

            ViewBag.TokenInfo = tokenInfo;
            ViewBag.RequestScheme = Request.Scheme;
            ViewBag.RequestHost = Request.Host.ToString();
            ViewBag.RequestPath = Request.Path;
            
            return Content($@"
=== TEACHER DEBUG INFO ===
IsAuthenticated: {ViewBag.IsAuthenticated}
UserName: {ViewBag.UserName}
AuthenticationType: {ViewBag.AuthenticationType}
HasCookie: {ViewBag.HasCookie}
CookieLength: {ViewBag.CookieLength}
Claims Count: {claimsList?.Count ?? 0}

Claims:
{string.Join("\n", claimsList?.Select(c => $"  {c.Type} = {c.Value}") ?? new List<string>())}

Token Info:
{ViewBag.TokenInfo}

Request Info:
{ViewBag.RequestScheme}://{ViewBag.RequestHost}{ViewBag.RequestPath}
", "text/plain");
        }

        /// <summary>
        /// Teacher Dashboard - Shows teacher statistics and overview
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                var stats = await _teacherService.GetTeacherStatsAsync(teacherId);
                ViewData["Title"] = "Teacher Dashboard";
                ViewData["TeacherId"] = teacherId;
                return View(stats);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load dashboard: {ex.Message}";
                return View(new TeacherStatsDto());
            }
        }

        /// <summary>
        /// Mark Attendance Page - Shows student list for a specific course
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MarkAttendance(int courseId)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                
                // Verify that the course belongs to the current teacher
                var assignedCourses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                var course = assignedCourses.FirstOrDefault(c => c.CourseId == courseId);
                
                if (course == null)
                {
                    TempData["Error"] = "You are not authorized to manage attendance for this course.";
                    return RedirectToAction("Index");
                }

                var students = await _teacherService.GetEnrolledStudentsAsync(courseId);
                
                ViewData["Title"] = $"Mark Attendance - {course.CourseName}";
                ViewData["CourseId"] = courseId;
                ViewData["CourseName"] = course.CourseName;
                ViewData["CourseCode"] = course.CourseCode;
                ViewData["TeacherId"] = teacherId;
                
                return View(students);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load student list: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ==================== API ENDPOINTS FOR AJAX CALLS ====================

        /// <summary>
        /// Get teacher's assigned courses
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAssignedCourses()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                var courses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                return Json(new { success = true, data = courses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get enrolled students for a specific course
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEnrolledStudents(int courseId)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                
                // Verify course ownership
                var assignedCourses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                if (!assignedCourses.Any(c => c.CourseId == courseId))
                {
                    return Json(new { success = false, error = "Unauthorized access to course" });
                }

                var students = await _teacherService.GetEnrolledStudentsAsync(courseId);
                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Submit attendance for multiple students
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitAttendance([FromBody] AttendanceSubmissionDto attendance)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                
                // Ensure the teacher ID in the request matches the current user
                attendance.TeacherId = teacherId;
                
                // Verify course ownership
                var assignedCourses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                if (!assignedCourses.Any(c => c.CourseId == attendance.CourseId))
                {
                    return Json(new { success = false, error = "Unauthorized access to course" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, error = "Validation failed", details = errors });
                }

                var result = await _teacherService.SubmitAttendanceAsync(attendance);
                
                if (result)
                {
                    TempData["Success"] = "Attendance submitted successfully!";
                    return Json(new { success = true, message = "Attendance submitted successfully" });
                }
                else
                {
                    return Json(new { success = false, error = "Failed to submit attendance" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get teacher statistics for dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTeacherStats()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                var stats = await _teacherService.GetTeacherStatsAsync(teacherId);
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get attendance history for a specific course with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAttendanceHistory(int courseId, DateTime? startDate = null, DateTime? endDate = null, string status = null)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                
                // Verify course ownership
                var assignedCourses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                if (!assignedCourses.Any(c => c.CourseId == courseId))
                {
                    return Json(new { success = false, error = "Unauthorized access to course" });
                }

                // For now, return placeholder data
                // In a real implementation, you would add this method to ITeacherService and TeacherService
                var placeholderData = new
                {
                    courseId = courseId,
                    totalRecords = 15,
                    summary = new
                    {
                        totalClasses = 15,
                        avgAttendance = 82.5,
                        totalStudents = 25
                    },
                    records = new[]
                    {
                        new { date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"), present = 20, absent = 3, late = 2 },
                        new { date = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"), present = 22, absent = 2, late = 1 },
                        new { date = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd"), present = 19, absent = 4, late = 2 }
                    }
                };

                return Json(new { success = true, data = placeholderData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ==================== COURSE MANAGEMENT ====================

        /// <summary>
        /// View all assigned courses
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Courses()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                var courses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                ViewData["Title"] = "My Courses";
                ViewData["TeacherId"] = teacherId;
                return View(courses);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load courses: {ex.Message}";
                return View(new List<TeacherCourseDto>());
            }
        }

        /// <summary>
        /// View students in a specific course
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CourseStudents(int courseId)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                
                // Verify course ownership
                var assignedCourses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                var course = assignedCourses.FirstOrDefault(c => c.CourseId == courseId);
                
                if (course == null)
                {
                    TempData["Error"] = "Course not found or you don't have access to it.";
                    return RedirectToAction("Courses");
                }

                var students = await _teacherService.GetEnrolledStudentsAsync(courseId);
                
                ViewData["Title"] = $"Students - {course.CourseName}";
                ViewData["CourseId"] = courseId;
                ViewData["CourseName"] = course.CourseName;
                ViewData["CourseCode"] = course.CourseCode;
                ViewData["TeacherId"] = teacherId;
                
                return View(students);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load students: {ex.Message}";
                return RedirectToAction("Courses");
            }
        }

        // ==================== ATTENDANCE MANAGEMENT ====================

        /// <summary>
        /// View attendance reports and analytics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                var stats = await _teacherService.GetTeacherStatsAsync(teacherId);
                
                ViewData["Title"] = "Attendance Reports";
                ViewData["TeacherId"] = teacherId;
                
                return View(stats);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load reports: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// View attendance history for a course
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AttendanceHistory(int courseId)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                
                // Verify course ownership
                var assignedCourses = await _teacherService.GetAssignedCoursesAsync(teacherId);
                var course = assignedCourses.FirstOrDefault(c => c.CourseId == courseId);
                
                if (course == null)
                {
                    TempData["Error"] = "Course not found or you don't have access to it.";
                    return RedirectToAction("Courses");
                }

                ViewData["Title"] = $"Attendance History - {course.CourseName}";
                ViewData["Course"] = course;
                ViewData["CourseId"] = courseId;
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load attendance history: {ex.Message}";
                return RedirectToAction("Courses");
            }
        }
    }
}