using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace AichatBot3.Service
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Retrieve SMTP settings from configuration
                var smtpServer = _config["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_config["EmailSettings:Port"]);
                var smtpUsername = _config["EmailSettings:Username"];
                var smtpPassword = _config["EmailSettings:Password"];
                var fromEmail = _config["EmailSettings:From"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("SMTP settings are not correctly configured.");
                    throw new InvalidOperationException("SMTP settings are missing or incorrect.");
                }

                // Prepare the email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("No Reply", fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                // Initialize and configure SMTP client
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);

                    // Send the email
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                // Log the success
                _logger.LogInformation("Email sent to {Email} successfully.", email);
            }
            catch (Exception ex)
            {
                // Log any errors encountered
                _logger.LogError(ex, "An error occurred while sending email to {Email}.", email);
                throw; // Optionally rethrow the exception or handle accordingly
            }
        }
    }
}
