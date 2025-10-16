using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

//[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }
}
