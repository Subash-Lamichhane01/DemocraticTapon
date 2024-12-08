using DemocraticTapON.Models;
using Microsoft.AspNetCore.Mvc;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(AccountModel model)
    {
        // Only validate username and password for login
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.FirstName));
        ModelState.Remove(nameof(model.LastName));

        if (ModelState.IsValid)
        {
            // Perform login validation
            // Check username and password against database
            return RedirectToAction("Index", "Home");
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Signup(AccountModel model)
    {
        if (ModelState.IsValid)
        {
            // Signup logic
            // - Check if username already exists
            // - Hash password
            // - Save to database
            return RedirectToAction("Login");
        }
        return View(model);
    }
}