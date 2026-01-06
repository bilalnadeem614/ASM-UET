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
            Debug.WriteLine("🚪 Logging out");
            Response.Cookies.Delete("ASM_TOKEN");
            TempData["Success"] = "Logged out successfully";
            return RedirectToAction("LoginPage");
        }
    }
}
