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
                

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }
    }
}
