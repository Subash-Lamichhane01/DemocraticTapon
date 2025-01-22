using DemocraticTapON.Data;
using DemocraticTapON.Models;
using DemocraticTapON.Models.ViewModels;
using DemocraticTapON.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly ApplicationDbContext _context;

    public AccountController(IAccountService accountService, ApplicationDbContext context)
    {
        _accountService = accountService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            var authProperties = await HttpContext.GetTokenAsync("initial_login");
            if (authProperties != "true")
            {
                return RedirectToAction("Index", "Home");
            }
        }

        TempData.Remove("PendingUserId");
        TempData.Remove("UserEmail");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var (success, error) = await _accountService.ValidateLoginAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }



            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Username == model.Username);


#if DEBUG
            // Skip email verification in development
            var principal = await _accountService.CreateUserClaimsPrincipalAsync(user);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectBasedOnRole(user.Role);
        #else
            var verificationCode = await _accountService.GenerateAndSendVerificationCodeAsync(user.Email);


            // Store verification details in TempData
            TempData["PendingUserId"] = user.Id;
            TempData["VerificationCode"] = verificationCode;
            TempData["UserEmail"] = user.Email;
            TempData["VerificationStartTime"] = DateTime.UtcNow.ToString("O");

            TempData.Keep();
            return RedirectToAction("VerifyEmail");
#endif
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred during login");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult VerifyEmail()
    {
        var userId = TempData.Peek("PendingUserId")?.ToString();
        var userEmail = TempData.Peek("UserEmail")?.ToString();
        var verificationStartTime = TempData.Peek("VerificationStartTime")?.ToString();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login");
        }

        if (verificationStartTime != null)
        {
            var startTime = DateTime.Parse(verificationStartTime);
            if (DateTime.UtcNow.Subtract(startTime).TotalMinutes > 10)
            {
                TempData["ErrorMessage"] = "Verification session expired. Please login again.";
                return RedirectToAction("Login");
            }
        }

        TempData.Keep();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEmail(string code)
    {
        try
        {
            var storedCode = TempData["VerificationCode"]?.ToString();
            var userId = TempData["PendingUserId"]?.ToString();

            if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Verification session expired. Please login again.";
                return RedirectToAction("Login");
            }

            var isVerified = _accountService.VerifyCode(storedCode, code);
            if (!isVerified)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code");
                TempData.Keep();
                return View();
            }

            var user = await _context.Accounts.FindAsync(Convert.ToInt32(userId));
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var principal = await _accountService.CreateUserClaimsPrincipalAsync(user);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            TempData.Clear();
            return RedirectBasedOnRole(user.Role);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred during verification. Please try again.";
            return RedirectToAction("Login");
        }
    }


    private IActionResult RedirectBasedOnRole(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Admin"),
            UserRole.Representative => RedirectToAction("Index", "Representative"),
            UserRole.User => RedirectToAction("Index", "Home"),
            _ => RedirectToAction("Index", "Home"),
        };
    }


    [HttpGet]
    public IActionResult Signup()
    {
        return View(new SignupViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Signup(SignupViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var (success, error) = await _accountService.ValidateSignupAsync(viewModel);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(viewModel);
            }

            var verificationCode = await _accountService.GenerateAndSendVerificationCodeAsync(viewModel.Email);

            // Store signup details and verification code in TempData
            TempData["PendingSignup"] = JsonSerializer.Serialize(viewModel);
            TempData["VerificationCode"] = verificationCode;
            TempData["VerificationStartTime"] = DateTime.UtcNow.ToString("O");
            TempData.Keep();

            return RedirectToAction("VerifySignup");
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "An error occurred during signup");
            return View(viewModel);
        }
    }

    [HttpGet]
    public IActionResult VerifySignup()
    {
        var signupJson = TempData.Peek("PendingSignup")?.ToString();
        var verificationStart = TempData.Peek("VerificationStartTime")?.ToString();

        if (string.IsNullOrEmpty(signupJson))
        {
            TempData["ErrorMessage"] = "Signup session expired. Please try again.";
            return RedirectToAction("Signup");
        }

        if (verificationStart != null)
        {
            var startTime = DateTime.Parse(verificationStart);
            if (DateTime.UtcNow.Subtract(startTime).TotalMinutes > 10)
            {
                TempData["ErrorMessage"] = "Verification session expired. Please try again.";
                return RedirectToAction("Signup");
            }
        }

        TempData.Keep();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifySignup(string code)
    {
        try
        {
            var signupJson = TempData["PendingSignup"]?.ToString();
            var storedCode = TempData["VerificationCode"]?.ToString();

            if (string.IsNullOrEmpty(signupJson) || string.IsNullOrEmpty(storedCode))
            {
                TempData["ErrorMessage"] = "Signup session expired. Please try again.";
                return RedirectToAction("Signup");
            }

            var viewModel = JsonSerializer.Deserialize<SignupViewModel>(signupJson);

            if (!_accountService.VerifyCode(storedCode, code))
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code");
                TempData.Keep();
                return View();
            }

            var account = await _accountService.CreateUserAccountAsync(viewModel);
            var principal = await _accountService.CreateUserClaimsPrincipalAsync(account, true);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            TempData.Clear();
            TempData["SuccessMessage"] = "Account created successfully! Welcome!";
            return RedirectBasedOnRole(account.Role);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred during signup. Please try again.";
            return RedirectToAction("Signup");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}