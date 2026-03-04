using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts.Authentication;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Logic.Authentication
{
	public class EmailService : IEmailService
	{
		private readonly EmailSettings _emailSettings;
		private readonly ILogger<EmailService> _logger;

		public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
		{
			_emailSettings = emailSettings.Value;
			_logger = logger;
		}

		public async Task<bool> SendVerificationCodeAsync(string email, string code)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.MailLogin));
				message.To.Add(new MailboxAddress("", email));

				message.Subject = "Код для входа в аккаунт";
				
				var bodyBuilder = new BodyBuilder();

				bodyBuilder.TextBody = $"Ваш код для входа: {code}\n\n" +
										"Код действителен 15 минут.\n" +
										"Если вы не запрашивали вход, проигнорируйте это письмо.";
				

				message.Body = bodyBuilder.ToMessageBody();

				using var client = new MailKit.Net.Smtp.SmtpClient();
				var socketOptions = _emailSettings.SmtpClientPort == 465
					? SecureSocketOptions.SslOnConnect
					: SecureSocketOptions.StartTls;
				await client.ConnectAsync(_emailSettings.SmtpClientHost,
										  _emailSettings.SmtpClientPort,
										  socketOptions);
				await client.AuthenticateAsync(_emailSettings.MailLogin, 
											   _emailSettings.MailPassword);
				await client.SendAsync(message);
				await client.DisconnectAsync(true);

				_logger.LogInformation($"Email sent to {email}");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка отправки письма на {Email}", email);
				return false;
			}
		}
	}
}
