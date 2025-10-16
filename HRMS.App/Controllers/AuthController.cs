using HRMS.Application.Features.Auth.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class AuthController : Controller
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string signinEmail, string signinPassword)
    {
        if (string.IsNullOrEmpty(signinEmail) || string.IsNullOrEmpty(signinPassword))
        {
            ViewBag.Error = "Please enter both Email and Password";
            return View();
        }

        var response = await _mediator.Send(new LoginCommand(signinEmail, signinPassword));

        if (response.IsSuccess)
        {
            // Save JWT in session
            HttpContext.Session.SetString("JWToken", response.Token);

            // Optionally, store User info in session
            HttpContext.Session.SetString("UserFullName", response.User.FullName);
            HttpContext.Session.SetString("UserRole", response.User.Role);

            return RedirectToAction("Index", "Dashboard");
        }
        else
        {
            // Failed login
            ViewBag.Error = response.Message;
            return View();
        }
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
