using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics;
using System.Security.Claims;

namespace ASM_UET.Attributes
{
    /// <summary>
    /// Authorization filter that checks if user has teacher role (1)
    /// This runs AFTER authentication, so User.Identity.IsAuthenticated will be set
    /// </summary>
    public class TeacherOnlyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var request = context.HttpContext.Request;
            
            // Force authentication to complete if not already done
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                // Try to authenticate using the authentication middleware
                var authenticateResult = await context.HttpContext.AuthenticateAsync();
                
                Debug.WriteLine("=== TeacherOnly Authorization Filter (Async) ===");
                Debug.WriteLine($"Path: {request.Path}");
                Debug.WriteLine($"Initial IsAuthenticated: False");
                Debug.WriteLine($"AuthenticateResult.Succeeded: {authenticateResult.Succeeded}");
                
                if (authenticateResult.Succeeded)
                {
                    // Update the user principal
                    context.HttpContext.User = authenticateResult.Principal;
                    user = authenticateResult.Principal;
                    Debug.WriteLine($"After manual auth - IsAuthenticated: {user.Identity?.IsAuthenticated}");
                }
            }
            
            Debug.WriteLine("=== TeacherOnly Authorization Filter ===");
            Debug.WriteLine($"Path: {request.Path}");
            Debug.WriteLine($"Has ASM_TOKEN cookie: {request.Cookies.ContainsKey("ASM_TOKEN")}");
            Debug.WriteLine($"Cookie value length: {request.Cookies["ASM_TOKEN"]?.Length ?? 0}");
            Debug.WriteLine($"User.Identity.IsAuthenticated: {user.Identity?.IsAuthenticated}");
            Debug.WriteLine($"User.Identity.AuthenticationType: {user.Identity?.AuthenticationType}");
            Debug.WriteLine($"Claims count: {user.Claims.Count()}");
            
            if (user.Claims.Any())
            {
                Debug.WriteLine("All Claims:");
                foreach (var claim in user.Claims)
                {
                    Debug.WriteLine($"  Type: '{claim.Type}' = Value: '{claim.Value}'");
                }
                
                // Check for different possible role claim types
                var roleClaimCustom = user.Claims.FirstOrDefault(c => c.Type == "role");
                var roleClaimStandard = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var roleClaimHttp = user.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                
                Debug.WriteLine($"Custom 'role' claim: {roleClaimCustom?.Value ?? "NULL"}");
                Debug.WriteLine($"ClaimTypes.Role claim: {roleClaimStandard?.Value ?? "NULL"}");
                Debug.WriteLine($"HTTP role claim: {roleClaimHttp?.Value ?? "NULL"}");
            }
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                Debug.WriteLine("? User NOT authenticated - redirecting to login");
                context.Result = new RedirectToActionResult("LoginPage", "Login", null);
                return;
            }

            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            Debug.WriteLine($"Role claim value: {roleClaim?.Value ?? "NULL"}");
            
            if (roleClaim == null || roleClaim.Value != "1")
            {
                Debug.WriteLine($"? Access DENIED - Role is {roleClaim?.Value ?? "NULL"}, expected 1");
                context.Result = new ContentResult
                {
                    Content = $"Access Denied: Teacher privileges required. Your role: {roleClaim?.Value ?? "NULL"}, Claims count: {user.Claims.Count()}",
                    StatusCode = 403
                };
                return;
            }
            
            Debug.WriteLine("? Access GRANTED - User is teacher");
        }
    }
}