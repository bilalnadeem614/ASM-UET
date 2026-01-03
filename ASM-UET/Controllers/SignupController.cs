using Microsoft.AspNetCore.Mvc;
using ASM_UET.Models;
using System.Net.Http.Headers;

namespace ASM_UET.Controllers
{
    public class SignupController : Controller
    {
        public IActionResult SignupPage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register_Page([FromForm] RegisterRequest model)
        {
            // call internal API
            using var client = new HttpClient();
            var baseUrl = Url.Content("~/");
            var form = new MultipartFormDataContent
            {
                { new StringContent(model.Role), "Role" },
                { new StringContent(model.FullName), "FullName" },
                { new StringContent(model.Email), "Email" },
                { new StringContent(model.Password), "Password" }
            };

            var resp = await client.PostAsync(new Uri(new Uri(Request.Scheme + "://" + Request.Host), "api/auth/register"), form);
            if (!resp.IsSuccessStatusCode)
            {
                var text = await resp.Content.ReadAsStringAsync();
                TempData["Error"] = text;
                return View("SignupPage");
            }

            var result = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (result == null) return View("SignupPage");

            // store token in cookie for demo
            Response.Cookies.Append("ASM_TOKEN", result.Token, new CookieOptions { HttpOnly = true });

            // redirect by role
            if (result.Role == 0) return RedirectToAction("Index", "Admin");
            if (result.Role == 1) return RedirectToAction("Index", "Teacher");
            return RedirectToAction("Index", "Student");
        }
    }
}
