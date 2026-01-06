using Microsoft.AspNetCore.Mvc;
using ASM_UET.Attributes;
using ASM_UET.Services;
using System.Security.Claims;
using System.Diagnostics;

namespace ASM_UET.Controllers
{
    [StudentOnly]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        /// <summary>
        /// Student personal dashboard showing attendance statistics and recent activity
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var dashboard = await _studentService.GetStudentDashboardAsync(studentId);
                return View(dashboard);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Student Index: {ex.Message}");
                TempData["Error"] = $"Error loading dashboard: {ex.Message}";
                return View();
            }
        }

        /// <summary>
        /// Shows student's current course enrollments with attendance details
        /// </summary>
        public async Task<IActionResult> Courses()
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var courses = await _studentService.GetStudentCoursesAsync(studentId);
                ViewBag.OverallAttendance = await _studentService.GetOverallAttendancePercentageAsync(studentId);
                return View(courses);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Student Courses: {ex.Message}");
                TempData["Error"] = $"Error loading courses: {ex.Message}";
                return View(new List<StudentCourseDto>());
            }
        }

        /// <summary>
        /// Shows available courses for registration
        /// </summary>
        public async Task<IActionResult> Register()
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var availableCourses = await _studentService.GetAvailableCoursesAsync(studentId);
                return View(availableCourses);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Student Register: {ex.Message}");
                TempData["Error"] = $"Error loading available courses: {ex.Message}";
                return View(new List<AvailableCourseDto>());
            }
        }

        /// <summary>
        /// Processes course enrollment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollInCourse(int courseId)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var success = await _studentService.EnrollInCourseAsync(studentId, courseId);
                
                if (success)
                {
                    TempData["Success"] = "Successfully enrolled in the course!";
                }
                else
                {
                    TempData["Error"] = "Failed to enroll in the course.";
                }
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"Argument error in EnrollInCourse: {ex.Message}");
                TempData["Error"] = "Invalid enrollment request. Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Invalid operation in EnrollInCourse: {ex.Message}");
                TempData["Warning"] = ex.Message; // Show specific validation message to user
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Authorization error in EnrollInCourse: {ex.Message}");
                TempData["Error"] = "You are not authorized to perform this action.";
                return RedirectToAction("LoginPage", "Login");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error in EnrollInCourse: {ex.Message}");
                TempData["Error"] = "An unexpected error occurred. Please try again later.";
            }

            return RedirectToAction("Register");
        }

        /// <summary>
        /// Shows detailed attendance history
        /// </summary>
        public async Task<IActionResult> AttendanceHistory()
        {
            try
            {
                var studentId = GetCurrentStudentId();
                var attendanceHistory = await _studentService.GetAttendanceHistoryAsync(studentId);
                return View(attendanceHistory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AttendanceHistory: {ex.Message}");
                TempData["Error"] = $"Error loading attendance history: {ex.Message}";
                return View(new List<StudentAttendanceHistoryDto>());
            }
        }

        /// <summary>
        /// Helper method to get current student ID from claims
        /// </summary>
        private int GetCurrentStudentId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int studentId))
            {
                throw new UnauthorizedAccessException("Unable to identify current student.");
            }
            return studentId;
        }
    }
}
