using Microsoft.AspNetCore.Mvc;
using ASM_UET.Models;
using ASM_UET.Services;
using System.Diagnostics;

namespace ASM_UET.Controllers
{
    public class LoginController : Controller
    {
        private readonly IAuthService _authService;

        public LoginController(IAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult LoginPage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login_Page([FromForm] LoginRequest model)
        {
            try
            {
                Debug.WriteLine($"🔑 Login attempt for: {model.Email}");
                
                var result = await _authService.LoginAsync(model);
                
                if (result == null)
                {
                    Debug.WriteLine($"❌ Login FAILED for: {model.Email}");
                    TempData["Error"] = "Invalid email or password";
                    return View("LoginPage");
                }

                Debug.WriteLine($"✅ Login SUCCESSFUL for: {model.Email}, Role: {result.Role}");

                // Store token in cookie with PROPER settings
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps, // TRUE for HTTPS, FALSE for HTTP
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(2),
                    Path = "/"
                };
                
                Response.Cookies.Append("ASM_TOKEN", result.Token, cookieOptions);
                
                Debug.WriteLine($"🍪 Cookie set - Secure: {cookieOptions.Secure}, IsHttps: {Request.IsHttps}");
                Debug.WriteLine($"🍪 Token length: {result.Token.Length}");

                // Redirect based on role
                var (action, controller) = result.Role switch
                {
                    0 => ("Index", "Admin"),
                    1 => ("Index", "Teacher"),
                    _ => ("Index", "Student")
                };

                Debug.WriteLine($"🔀 Redirecting to: /{controller}/{action}");
                return RedirectToAction(action, controller);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Login ERROR: {ex.Message}");
                TempData["Error"] = $"Login failed: {ex.Message}";
                return View("LoginPage");
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Debug.WriteLine("🚪 Secure logout initiated");
            
            try
            {
                // Clear the ASM_TOKEN cookie with proper settings
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps, // Match the original cookie settings
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1), // Expire the cookie
                    Path = "/"
                };
                
                Response.Cookies.Append("ASM_TOKEN", "", cookieOptions);
                
                // Alternative method to ensure cookie deletion
                Response.Cookies.Delete("ASM_TOKEN", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });
                
                Debug.WriteLine("🍪 JWT token cookie cleared successfully");
                
                // Clear any additional auth-related cookies if they exist
                foreach (var cookie in Request.Cookies.Keys)
                {
                    if (cookie.StartsWith("ASM_") || cookie.Contains("auth") || cookie.Contains("token"))
                    {
                        Response.Cookies.Delete(cookie);
                        Debug.WriteLine($"🧹 Cleared additional cookie: {cookie}");
                    }
                }
                
                // Clear TempData to remove any sensitive information
                TempData.Clear();
                
                // Add success message for logout
                TempData["Success"] = "You have been logged out successfully. Please login again to access the system.";
                
                Debug.WriteLine("✅ Secure logout completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error during logout: {ex.Message}");
                TempData["Error"] = "An error occurred during logout. Please close your browser for security.";
            }
            
            return RedirectToAction("LoginPage");
        }

        /// <summary>
        /// Enhanced logout action that provides additional security cleanup
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SecureLogout()
        {
            Debug.WriteLine("🔒 Enhanced secure logout initiated");
            
            try
            {
                // Get the token before clearing it for logging purposes
                var existingToken = Request.Cookies["ASM_TOKEN"];
                var tokenExists = !string.IsNullOrEmpty(existingToken);
                
                Debug.WriteLine($"🔍 Token exists: {tokenExists}, Length: {existingToken?.Length ?? 0}");
                
                // Clear all authentication cookies with multiple methods for thoroughness
                var cookiesToClear = new[] { "ASM_TOKEN", "ASM_AUTH", "ASM_SESSION", ".AspNetCore.Antiforgery", ".AspNetCore.Session" };
                
                foreach (var cookieName in cookiesToClear)
                {
                    if (Request.Cookies.ContainsKey(cookieName))
                    {
                        // Method 1: Set expired cookie
                        Response.Cookies.Append(cookieName, "", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = Request.IsHttps,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddYears(-1),
                            Path = "/"
                        });
                        
                        // Method 2: Explicit delete
                        Response.Cookies.Delete(cookieName, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = Request.IsHttps,
                            SameSite = SameSiteMode.Lax,
                            Path = "/"
                        });
                        
                        Debug.WriteLine($"🧹 Cleared cookie: {cookieName}");
                    }
                }
                
                // Clear server-side session if it exists
                if (HttpContext.Session != null)
                {
                    HttpContext.Session.Clear();
                    Debug.WriteLine("🗑️ Server session cleared");
                }
                
                // Clear TempData and ViewData
                TempData.Clear();
                ViewData.Clear();
                
                // Invalidate any authentication state
                if (HttpContext.User?.Identity?.IsAuthenticated == true)
                {
                    Debug.WriteLine("🔓 User was authenticated, state will be cleared");
                }
                
                TempData["Success"] = "Logout completed successfully. All authentication data has been cleared.";
                Debug.WriteLine("✅ Enhanced secure logout completed");
                
                return Json(new { success = true, message = "Logout successful", redirectUrl = Url.Action("LoginPage") });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error during secure logout: {ex.Message}");
                return Json(new { success = false, error = "Logout failed. Please close your browser." });
            }
        }
    }
}
