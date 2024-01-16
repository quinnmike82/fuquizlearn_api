using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace fuquizlearn_api.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html, string from = null);
    }

    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;

        public EmailService(ISendGridClient sendGridClient)
        {
            _sendGridClient = sendGridClient ?? throw new ArgumentNullException(nameof(sendGridClient));
        }

        public async Task SendAsync(string to, string subject, string html, string from = null)
        {
            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentException("The 'to' parameter cannot be null or empty.", nameof(to));
            }

            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentException("The 'subject' parameter cannot be null or empty.", nameof(subject));
            }

            var msg = new SendGridMessage
            {
                From = new EmailAddress(from ?? "ngocvlqt1995@gmail.com", "QuizLearn"),
                Subject = subject
            };

            msg.AddContent(MimeType.Html, html);
            msg.AddTo(new EmailAddress(to));

            try
            {
                var response = await _sendGridClient.SendEmailAsync(msg).ConfigureAwait(false);

                // Log or handle the response as needed
                Console.WriteLine($"SendGrid response: {response.StatusCode}");
                Console.WriteLine($"Headers: {response.Headers}");
                Console.WriteLine($"Body: {response.Body}");
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw; // Rethrow the exception if needed
            }
        }
    }
}
