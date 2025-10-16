using Microsoft.AspNetCore.Mvc;

namespace PayRoll.App.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
