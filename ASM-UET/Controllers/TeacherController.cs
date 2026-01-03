using Microsoft.AspNetCore.Mvc;

namespace ASM_UET.Controllers
{
    public class TeacherController : Controller
    {
        public IActionResult Index()
        {
            return Content("Teacher Dashboard - dummy. Role=1");
        }
    }
}
