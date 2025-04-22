using FishCareSystem.API.Services.Interface;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace FishCareSystem.API.Services.Service
{

    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey is not configured");
            _senderEmail = configuration["SendGrid:SenderEmail"] ?? throw new ArgumentNullException("SendGrid:SenderEmail is not configured");
            _senderName = configuration["SendGrid:SenderName"] ?? "FishCare System";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Failed to send email: {response.StatusCode}");
            }
        }
    }
}
