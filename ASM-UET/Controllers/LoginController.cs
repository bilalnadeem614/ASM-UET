using Microsoft.AspNetCore.Mvc;

namespace ASM_UET.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
