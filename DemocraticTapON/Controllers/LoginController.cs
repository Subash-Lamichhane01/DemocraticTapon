using DemocraticTapON.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemocraticTapON.Controllers
{
    public class LoginController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                // Implement your login logic here
                // e.g., check the username and password against a database or authentication service

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }
    }
}
