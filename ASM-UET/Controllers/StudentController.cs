using Microsoft.AspNetCore.Mvc;
using ASM_UET.Attributes;
using ASM_UET.Services;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

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
                Debug.WriteLine($"[StudentController] EnrollInCourse called - Student ID: {studentId}, Course ID: {courseId}");
                
                var success = await _studentService.EnrollInCourseAsync(studentId, courseId);
                
                if (success)
                {
                    Debug.WriteLine($"[StudentController] Enrollment successful for Student {studentId} in Course {courseId}");
                    TempData["Success"] = "Successfully enrolled in the course!";
                }
                else
                {
                    Debug.WriteLine($"[StudentController] Enrollment returned false for Student {studentId} in Course {courseId}");
                    TempData["Error"] = "Failed to enroll in the course.";
                }
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"[StudentController] ArgumentException in EnrollInCourse: {ex.Message}");
                TempData["Error"] = $"Invalid request: {ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[StudentController] InvalidOperationException in EnrollInCourse: {ex.Message}");
                TempData["Warning"] = ex.Message; // Show specific validation message to user
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[StudentController] UnauthorizedAccessException in EnrollInCourse: {ex.Message}");
                TempData["Error"] = "You are not authorized to perform this action.";
                return RedirectToAction("LoginPage", "Login");
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"[StudentController] DbUpdateException in EnrollInCourse: {ex.Message}");
                Debug.WriteLine($"[StudentController] Inner Exception: {ex.InnerException?.Message}");
                TempData["Error"] = "Database error during enrollment. Please contact support if this persists.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentController] Unexpected Exception Type: {ex.GetType().Name}");
                Debug.WriteLine($"[StudentController] Unexpected Exception in EnrollInCourse: {ex.Message}");
                Debug.WriteLine($"[StudentController] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[StudentController] Inner Exception: {ex.InnerException.Message}");
                }
                
                // Provide more specific error message based on exception details
                if (ex.Message.Contains("Database error"))
                {
                    TempData["Error"] = "Database connection error. Please try again later.";
                }
                else if (ex.Message.Contains("timeout"))
                {
                    TempData["Error"] = "Request timeout. Please try again.";
                }
                else
                {
                    TempData["Error"] = $"An error occurred: {ex.Message}. Please contact support if this persists.";
                }
            }

            return RedirectToAction("Register");
        }

        /// <summary>
        /// Diagnostic action to test enrollment process step by step
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEnrollment(int courseId)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                Debug.WriteLine($"[TestEnrollment] Starting diagnostic test for Student ID: {studentId}, Course ID: {courseId}");
                
                // Test 1: Check if student exists
                var student = await _studentService.GetStudentDashboardAsync(studentId);
                Debug.WriteLine($"[TestEnrollment] Student found: {student.StudentName}");
                
                // Test 2: Check if course exists in available courses
                var availableCourses = await _studentService.GetAvailableCoursesAsync(studentId);
                var targetCourse = availableCourses.FirstOrDefault(c => c.CourseId == courseId);
                
                if (targetCourse == null)
                {
                    TempData["Error"] = "Course is not available for enrollment or you may already be enrolled.";
                    return RedirectToAction("Register");
                }
                
                Debug.WriteLine($"[TestEnrollment] Target course found: {targetCourse.CourseCode} - {targetCourse.CourseName}");
                
                // Test 3: Try the actual enrollment
                var success = await _studentService.EnrollInCourseAsync(studentId, courseId);
                
                if (success)
                {
                    TempData["Success"] = $"Successfully enrolled in {targetCourse.CourseCode} - {targetCourse.CourseName}!";
                }
                else
                {
                    TempData["Error"] = "Enrollment failed - service returned false.";
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TestEnrollment] Exception: {ex.Message}");
                TempData["Error"] = $"Test enrollment failed: {ex.Message}";
            }
            
            return RedirectToAction("Register");
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
