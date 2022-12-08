using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using TheBlogProject.ViewModels;

namespace TheBlogProject.Services
{
    public class EmailService : IBlogEmailSender
    {
        private readonly MailSettings _mailSettings;

        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendContactEmailAsync(string emailFrom, string name, string subject, string htmlMessage)
        {
            var emailSender = _mailSettings.Email ?? Environment.GetEnvironmentVariable("Email");
            MimeMessage newEmail = new();
            newEmail.Sender = MailboxAddress.Parse(emailSender);
            newEmail.To.Add(MailboxAddress.Parse(emailSender));

            newEmail.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = $"<b>{name}</b> has sent you an email and can be reached at: <b>{emailFrom}</b><br/><br/>{htmlMessage}";

            newEmail.Body = builder.ToMessageBody();

            using MailKit.Net.Smtp.SmtpClient smtpClient = new();

            try
            {
                var host = _mailSettings.MailHost ?? Environment.GetEnvironmentVariable("MailHost");
                var port = _mailSettings.MailPort != 0 ? _mailSettings.MailPort : int.Parse(Environment.GetEnvironmentVariable("MailPort"));
                var password = _mailSettings.MailPassword ?? Environment.GetEnvironmentVariable("MailPassword");

                await smtpClient.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(emailSender, password);

                await smtpClient.SendAsync(newEmail);
                await smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                throw;
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailSender = _mailSettings.Email ?? Environment.GetEnvironmentVariable("Email");

            MimeMessage newEmail = new();

            newEmail.Sender = MailboxAddress.Parse(emailSender);

            // There could be more than one recipient, hence the loop
            foreach (var emailAddress in email.Split(";"))
            {
                newEmail.To.Add(MailboxAddress.Parse(emailAddress));
            }

            newEmail.Subject = subject;

            BodyBuilder emailBody = new();
            emailBody.HtmlBody = htmlMessage;

            newEmail.Body = emailBody.ToMessageBody();

            // At this point lets log in to our smtp client!
            using MailKit.Net.Smtp.SmtpClient smtpClient = new();

            try
            {
                var host = _mailSettings.MailHost ?? Environment.GetEnvironmentVariable("MailHost");
                var port = _mailSettings.MailPort != 0 ? _mailSettings.MailPort : int.Parse(Environment.GetEnvironmentVariable("MailPort"));
                var password = _mailSettings.MailPassword ?? Environment.GetEnvironmentVariable("MailPassword");

                await smtpClient.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(emailSender, password);

                await smtpClient.SendAsync(newEmail);
                await smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                throw;
            }

            //var email = new MimeMessage();
            //email.Sender = MailboxAddress.Parse(_mailSettings.Email);
            //email.To.Add(MailboxAddress.Parse(emailTo));
            //email.Subject = subject;


            //var builder = new BodyBuilder()
            //{
            //    HtmlBody = htmlMessage
            //};

            //email.Body = builder.ToMessageBody();

            //using var smtp = new SmtpClient();

            //var host = _mailSettings.MailHost ?? Environment.GetEnvironmentVariable("MailHost");
            //var port = _mailSettings.MailPort != 0 ? _mailSettings.MailPort : int.Parse(Environment.GetEnvironmentVariable("MailPort"));
            //var password = _mailSettings.MailPassword ?? Environment.GetEnvironmentVariable("MailPassword");

            //smtp.Connect(host, port, SecureSocketOptions.StartTls);
            //smtp.Authenticate(_mailSettings.Email, _mailSettings.MailPassword);

            //await smtp.SendAsync(email);

            //smtp.Disconnect(true);
        }
    }
}
