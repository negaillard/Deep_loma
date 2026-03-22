using Contracts.BindingModels.Authentication;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using MailKit.Security;
using MassTransit;
using MessageContracts;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Consumers
{
	/// <summary>
	/// Обеспечивает отправку уведомлений на почту
	/// </summary>
	public class NotificationConsumer : IConsumer<NotificationMessage>
	{
		private readonly ILogger<NotificationConsumer> _logger;
		private readonly IUserStorage _userStorage;
		private readonly EmailSettings _emailSettings;

		public NotificationConsumer(
			ILogger<NotificationConsumer> logger, 
			IUserStorage userStorage,
			IOptions<EmailSettings> emailSettings)
		{
			_emailSettings = emailSettings.Value;
			_logger = logger;
			_userStorage = userStorage;
		}
		public async Task Consume(ConsumeContext<NotificationMessage> context)
		{
			var message = context.Message;
			_logger.LogInformation(
				"Получен запрос на отправку письма: UserId={UserId}, Title={Title}",
				message.UserId, message.Title);
			try
			{
				var user = await _userStorage.GetElementAsync(new UserSearchModel {
					Id = message.UserId });

				if (user == null)
				{
					_logger.LogWarning("Пользователь {UserId} не найден или удалён", message.UserId);
					return;
				}
				var email = user.Email;

				_logger.LogInformation(
				"Попытка отправки письма-уведомления: Email={email} относительно документа Title={Title}",
				email, message.Title);

				await SendNotificationMessage(email, message.Title);
			}
			catch (Exception ex) {
				_logger.LogError(ex,
					"Ошибка при отправке письма-уведомления: UserId={UserId} относительно документа Title={Title}",
					message.UserId, message.Title);
			}
		}

		public async Task<bool> SendNotificationMessage(string email, string title)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.MailLogin));
				message.To.Add(new MailboxAddress("", email));

				message.Subject = "Вам пришли новые документы на подпись";

				var bodyBuilder = new BodyBuilder();

				bodyBuilder.TextBody = "Добрый день\n\n" +
										$"Документ |{title}| доступен для подписания в мобильном приложении.";


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
