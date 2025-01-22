using DemocraticTapON.Data;
using DemocraticTapON.Models.ViewModels;
using DemocraticTapON.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DemocraticTapON.Utilities
{
    public interface IAccountService
    {
        Task<(bool success, string error)> ValidateLoginAsync(LoginViewModel Loginmodel);
        Task<(bool success, string error)> ValidateSignupAsync(SignupViewModel Signupmodel);
        Task<string> GenerateAndSendVerificationCodeAsync(string email);
        bool VerifyCode(string storedCode, string providedCode);
        Task<ClaimsPrincipal> CreateUserClaimsPrincipalAsync(AccountModel user, bool isInitialLogin = false);
        Task<AccountModel> CreateUserAccountAsync(SignupViewModel model);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<(bool success, string error)> ValidateLoginAsync(LoginViewModel LoginModel)
        {
            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == LoginModel.Username);

            if (user == null)
            {
                return (false, "Invalid username or password");
            }

            if (!PasswordHasher.VerifyPassword(LoginModel.Password, user.Password))
            {
                return (false, "Invalid username or password");
            }

            return (true, null);
        }

        public async Task<(bool success, string error)> ValidateSignupAsync(SignupViewModel model)
        {
            var existingUser = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == model.Username);
            var existingPhone = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

            if (existingUser != null)
            {
                return (false, "Username is already taken");
            }

            if (existingPhone != null)
            {
                return (false, "Phone Number is already registered");
            }

            return (true, null);
        }

        public async Task<string> GenerateAndSendVerificationCodeAsync(string email)
        {
            var verificationCode = _emailService.GenerateVerificationCode();
            var codeSent = await _emailService.SendVerificationCodeAsync(email, verificationCode);

            if (!codeSent)
            {
                throw new Exception("Failed to send verification code");
            }

            return verificationCode;
        }

        public bool VerifyCode(string storedCode, string providedCode)
        {
            return _emailService.VerifyCode(storedCode, providedCode);
        }

        public async Task<ClaimsPrincipal> CreateUserClaimsPrincipalAsync(AccountModel user, bool isInitialLogin = false)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                IsPersistent = true
            };

            if (isInitialLogin)
            {
                authProperties.Items["initial_login"] = "true";
            }

            return new ClaimsPrincipal(claimsIdentity);
        }

        public async Task<AccountModel> CreateUserAccountAsync(SignupViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var account = new AccountModel
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = PasswordHasher.HashPassword(model.Password),
                    IsEmailVerified = true,
                    LastVerificationDate = DateTime.UtcNow,
                    Role = model.Role,
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    AccountId = account.Id,
                    Account = account,
                    UserBill = new List<UserBill>()
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (model.Role == UserRole.Representative)
                {
                    // Make sure we have the user's ID
                    var userId = user.UserId;  // This should now be populated

                    var representative = new Representative
                    {
                        UserId = userId,  // Use the actual UserId
                        Constituency = "Not Set",
                        State = "Not Set",
                        PoliticalParty = "Not Set",
                        TermStart = DateTime.UtcNow,
                        TermEnd = DateTime.UtcNow.AddYears(5),
                        OfficeAddress = "Not Set",
                        OfficePhone = null,
                        ProposedBills = new List<Bill>()
                    };

                    _context.Representatives.Add(representative);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return account;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
