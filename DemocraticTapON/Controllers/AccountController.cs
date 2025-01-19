using DemocraticTapON.Data;
using DemocraticTapON.Models;
using DemocraticTapON.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public AccountController(ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            var initialLogin = HttpContext.User.Identity as ClaimsIdentity;
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
    public async Task<IActionResult> Login(AccountModel model)
    {
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.FirstName));
        ModelState.Remove(nameof(model.LastName));
        ModelState.Remove(nameof(model.PhoneNumber));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }

            if (PasswordHasher.VerifyPassword(model.Password, user.Password))
            {
                // Generate verification code
                var verificationCode = _emailService.GenerateVerificationCode();

                // Store verification details in TempData
                TempData["PendingUserId"] = user.Id;
                TempData["VerificationCode"] = verificationCode;
                TempData["UserEmail"] = user.Email;
                TempData.Keep("PendingUserId");
                TempData.Keep("VerificationCode");
                TempData.Keep("UserEmail");

                // Add verification start time
                TempData["VerificationStartTime"] = DateTime.UtcNow.ToString("O");
                TempData.Keep("VerificationStartTime");

                // Send verification code via email
                var codeSent = await _emailService.SendVerificationCodeAsync(user.Email, verificationCode);
                if (!codeSent)
                {
                    ModelState.AddModelError(string.Empty, "Failed to send verification code");
                    return View(model);
                }

                return RedirectToAction("VerifyEmail");
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login Error: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred during login");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult VerifyEmail()
    {
        var userId = TempData.Peek("PendingUserId")?.ToString();
        var userEmail = TempData.Peek("UserEmail")?.ToString();
        var verificationStartTime = TempData.Peek("VerificationStartTime")?.ToString();

        if (userId == null || userEmail == null)
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

        TempData.Keep("PendingUserId");
        TempData.Keep("VerificationCode");
        TempData.Keep("UserEmail");
        TempData.Keep("VerificationStartTime");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEmail(string code)
    {
        try
        {
            var storedCode = TempData["VerificationCode"]?.ToString();
            var userId = TempData["PendingUserId"]?.ToString();
            var verificationStartTime = TempData["VerificationStartTime"]?.ToString();

            if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Verification session expired. Please login again.";
                return RedirectToAction("Login");
            }

            // Check verification timeout
            if (verificationStartTime != null)
            {
                var startTime = DateTime.Parse(verificationStartTime);
                if (DateTime.UtcNow.Subtract(startTime).TotalMinutes > 10)
                {
                    TempData["ErrorMessage"] = "Verification session expired. Please login again.";
                    return RedirectToAction("Login");
                }
            }

            var isVerified = _emailService.VerifyCode(storedCode, code);
            if (!isVerified)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code");
                TempData.Keep("PendingUserId");
                TempData.Keep("VerificationCode");
                TempData.Keep("UserEmail");
                TempData.Keep("VerificationStartTime");
                return View();
            }

            var user = await _context.Accounts.FindAsync(Convert.ToInt32(userId));
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Clear TempData after successful verification
            TempData.Remove("PendingUserId");
            TempData.Remove("VerificationCode");
            TempData.Remove("UserEmail");
            TempData.Remove("VerificationStartTime");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VerifyEmail Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            TempData["ErrorMessage"] = "An error occurred during verification. Please try again.";
            return RedirectToAction("Login");
        }
    }

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Signup(AccountModel model)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return View(model);
            }

            // Generate and send verification code
            var verificationCode = _emailService.GenerateVerificationCode();
            var codeSent = await _emailService.SendVerificationCodeAsync(model.Email, verificationCode);
            if (!codeSent)
            {
                ModelState.AddModelError(string.Empty, "Failed to send verification code");
                return View(model);
            }

            // Store signup details and verification code in TempData
            TempData["PendingSignup"] = System.Text.Json.JsonSerializer.Serialize(model);
            TempData["VerificationCode"] = verificationCode;
            TempData.Keep("PendingSignup");
            TempData.Keep("VerificationCode");

            TempData["VerificationStartTime"] = DateTime.UtcNow.ToString("O");
            TempData.Keep("VerificationStartTime");

            return RedirectToAction("VerifySignup");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult VerifySignup()
    {
        var modelJson = TempData.Peek("PendingSignup")?.ToString();
        if (string.IsNullOrEmpty(modelJson))
        {
            TempData["ErrorMessage"] = "Signup session expired. Please try again.";
            return RedirectToAction("Signup");
        }

        if (TempData["VerificationStartTime"] != null)
        {
            var startTime = DateTime.Parse(TempData["VerificationStartTime"].ToString());
            if (DateTime.UtcNow.Subtract(startTime).TotalMinutes > 10)
            {
                TempData["ErrorMessage"] = "Verification session expired. Please try again.";
                return RedirectToAction("Signup");
            }
        }

        TempData.Keep("PendingSignup");
        TempData.Keep("VerificationCode");
        TempData.Keep("VerificationStartTime");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifySignup(string code)
    {
        var modelJson = TempData["PendingSignup"]?.ToString();
        var storedCode = TempData["VerificationCode"]?.ToString();

        if (string.IsNullOrEmpty(modelJson) || string.IsNullOrEmpty(storedCode))
        {
            TempData["ErrorMessage"] = "Signup session expired. Please try again.";
            return RedirectToAction("Signup");
        }

        try
        {
            var model = System.Text.Json.JsonSerializer.Deserialize<AccountModel>(modelJson);

            // Verify the code
            var isVerified = _emailService.VerifyCode(storedCode, code);
            if (!isVerified)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code");
                TempData.Keep("PendingSignup");
                TempData.Keep("VerificationCode");
                TempData.Keep("VerificationStartTime");
                return View();
            }

            var existingUser = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Username is no longer available. Please choose another.";
                return RedirectToAction("Signup");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.Password = PasswordHasher.HashPassword(model.Password);
                model.IsEmailVerified = true;  // Consider renaming this to IsEmailVerified
                model.LastVerificationDate = DateTime.UtcNow;

                _context.Accounts.Add(model);
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.NameIdentifier, model.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                    IsPersistent = true,
                    Items = { { "initial_login", "true" } }
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Account created successfully! Welcome!";
                return RedirectToAction("Index", "Home");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VerifySignup Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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