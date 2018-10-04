using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentitySample.Services
{
	public interface ISmsSender
	{
		Task SendSmsAsync(string number, string message);
	}

	public class EmailSettings
	{
		public string DomainOrHost { get; set; }

		public int Port { get; set; }

		public string FromUsernameEmail { get; set; }

		public string FromUsernameDisplayName { get; set; }

		public string AuthUsername { get; set; }

		public string UsernamePassword { get; set; }

		public string ToEmail { get; set; }

		public string CcEmail { get; set; }

		public string BccEmail { get; set; }
	}

	// This class is used by the application to send Email and SMS
	// when you turn on two-factor authentication in ASP.NET Identity.
	// For more details see this link http://go.microsoft.com/fwlink/?LinkID=532713
	public class AuthMessageSender : IEmailSender, ISmsSender
	{
		private readonly EmailSettings _emailSettings;

		private readonly ILogger<AuthMessageSender> _logger;

		public AuthMessageSender(IOptions<EmailSettings> emailSettings, ILogger<AuthMessageSender> logger)
		{
			_emailSettings = emailSettings.Value;
			_logger = logger;
		}

		public async Task Execute(string email, string subject, string message)
		{
			try
			{
				string toEmail = string.IsNullOrEmpty(email)
								? _emailSettings.ToEmail
								: email;
				MailMessage mail = new MailMessage()
				{
					From = new MailAddress(_emailSettings.FromUsernameEmail, _emailSettings.FromUsernameDisplayName)
				};
				mail.To.Add(new MailAddress(toEmail));
				if (!string.IsNullOrEmpty(_emailSettings.CcEmail))
					mail.CC.Add(new MailAddress(_emailSettings.CcEmail));
				if (!string.IsNullOrEmpty(_emailSettings.BccEmail))
					mail.Bcc.Add(new MailAddress(_emailSettings.BccEmail));

				mail.Subject = $"Personal Management System - {subject}";
				mail.Body = message;
				mail.IsBodyHtml = true;
				mail.Priority = MailPriority.High;

				using (SmtpClient smtp = new SmtpClient(_emailSettings.DomainOrHost, _emailSettings.Port))
				{
					smtp.Credentials = new NetworkCredential(_emailSettings.AuthUsername, _emailSettings.UsernamePassword);
					smtp.EnableSsl = true;

					await smtp.SendMailAsync(mail).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "email error");
			}
		}

		public async Task SendEmailAsync(string email, string subject, string message)
		{
			// Plug in your email service here to send an email.
			if (_emailSettings != null && !string.IsNullOrEmpty(_emailSettings.DomainOrHost))
				await Execute(email, subject, message);
		}

		public Task SendSmsAsync(string number, string message)
		{
			// Plug in your SMS service here to send a text message.
			return Task.FromResult(0);
		}
	}

	/*public static class MessageServices
	{
		public static Task SendEmailAsync(string email, string subject, string message)
		{
			// Plug in your email service
			return Task.FromResult(0);
		}

		public static Task SendSmsAsync(string number, string message)
		{
			// Plug in your sms service
			return Task.FromResult(0);
		}

	}*/
}
