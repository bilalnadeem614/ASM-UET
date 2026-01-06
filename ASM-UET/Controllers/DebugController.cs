using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace ASM_UET.Controllers
{
    public class DebugController : Controller
    {
        [HttpGet]
        public IActionResult Auth()
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
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadToken(Request.Cookies["ASM_TOKEN"]) as JwtSecurityToken;
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

            // Check all cookies
            var allCookies = Request.Cookies.Select(c => $"{c.Key} = {c.Value.Substring(0, Math.Min(50, c.Value.Length))}...").ToList();
            ViewBag.AllCookies = allCookies;

            ViewBag.TokenInfo = tokenInfo;
            ViewBag.RequestScheme = Request.Scheme;
            ViewBag.RequestHost = Request.Host.ToString();
            ViewBag.RequestPath = Request.Path;
            
            return View();
        }

        [HttpGet]
        public IActionResult ClearCookies()
        {
            Response.Cookies.Delete("ASM_TOKEN");
            return Content("Cookies cleared. Go back to login.");
        }
    }
}
