using Microsoft.AspNetCore.Mvc;

namespace ASM_UET.Controllers
{
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return Content("Student Dashboard - dummy. Role=2");
        }
    }
}
