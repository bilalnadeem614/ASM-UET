using Microsoft.AspNetCore.Mvc;

namespace ASM_UET.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return Content("Admin Panel - dummy dashboard. Role=0");
        }
    }
}
