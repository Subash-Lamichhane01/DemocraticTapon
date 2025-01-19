using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

public interface IEmailService
{
    Task<bool> SendVerificationCodeAsync(string email, string code);
    bool VerifyCode(string storedCode, string providedCode);
}

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly Random _random;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _random = new Random();
    }

    public async Task<bool> SendVerificationCodeAsync(string email, string code)
    {
        try
        {
            using var client = new SmtpClient()
            {
                Host = _configuration["EmailSettings:SmtpHost"],
                Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["EmailSettings:Username"]),
                Subject = "Your Verification Code",
                Body = $"Your verification code is: {code}",
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            return false;
        }
    }

    public string GenerateVerificationCode()
    {
        return _random.Next(100000, 999999).ToString();
    }

    public bool VerifyCode(string storedCode, string providedCode)
    {
        return storedCode == providedCode;
    }
}
