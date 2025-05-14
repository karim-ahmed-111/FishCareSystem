using FishCareSystem.API.Services.Interface;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FishCareSystem.API.Services.Service
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var smtpClient = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
            {
                Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"]),
                EnableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true")
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSection["User"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
} 