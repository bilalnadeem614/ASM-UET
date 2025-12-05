using Microsoft.AspNetCore.Mvc;

namespace ASM_UET.Controllers
{
    public class SignupController : Controller
    {
        public IActionResult SignupPage()
        {
            return View();
        }
    }
}
