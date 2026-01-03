using Microsoft.AspNetCore.Mvc;
using ASM_UET.Models;

namespace ASM_UET.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult LoginPage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login_Page([FromForm] LoginRequest model)
        {
            using var client = new HttpClient();
            var form = new MultipartFormDataContent
            {
                { new StringContent(model.Email), "Email" },
                { new StringContent(model.Password), "Password" }
            };

            var resp = await client.PostAsync(new Uri(new Uri(Request.Scheme + "://" + Request.Host), "api/auth/login"), form);
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Invalid credentials";
                return View("LoginPage");
            }

            var result = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null)
            {
                TempData["Error"] = "Invalid server response";
                return View("LoginPage");
            }

            Response.Cookies.Append("ASM_TOKEN", result.Token, new CookieOptions { HttpOnly = true });

            if (result.Role == 0) return RedirectToAction("Index", "Admin");
            if (result.Role == 1) return RedirectToAction("Index", "Teacher");
            return RedirectToAction("Index", "Student");
        }
    }
}
