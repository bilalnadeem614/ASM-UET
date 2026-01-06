using Microsoft.AspNetCore.Mvc.RazorPages;
using ASM_UET.Services;
using ASM_UET.Models;
using ASM_UET.Attributes;

namespace ASM_UET.Pages.Admin
{
    [AdminOnly]
    public class IndexModel : PageModel
    {
        private readonly IAdminService _adminService;

        public IndexModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public DashboardStatsDto Stats { get; set; } = new();

        public async Task OnGet()
        {
            try
            {
                Stats = await _adminService.GetDashboardStatsAsync();
                ViewData["Title"] = "Dashboard";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to load dashboard: {ex.Message}";
                Stats = new DashboardStatsDto();
            }
        }
    }
}