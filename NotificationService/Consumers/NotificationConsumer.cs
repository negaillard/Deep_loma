using Contracts.BindingModels.Authentication;
using MailKit.Security;
using MassTransit;
using MessageContracts;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NotificationService.Consumers
{
	public class NotificationConsumer : IConsumer<NotificationMessage>
	{
		private readonly ILogger<NotificationConsumer> _logger;
		private readonly EmailSettings _emailSettings;

		public NotificationConsumer(
			ILogger<NotificationConsumer> logger,
			IOptions<EmailSettings> emailSettings)
		{
			_logger = logger;
			_emailSettings = emailSettings.Value;
		}

		public async Task Consume(ConsumeContext<NotificationMessage> context)
		{
			var notification = context.Message;

			_logger.LogInformation(
				"Получено уведомление для отправки: RecipientEmail={RecipientEmail}, RecipientName={RecipientName}, DocumentTitle={DocumentTitle}, RequestedByName={RequestedByName}, RequestedAt={RequestedAt:O}",
				notification.RecipientEmail,
				notification.RecipientName,
				notification.DocumentTitle,
				notification.RequestedByName,
				notification.RequestedAt);

			try
			{
				_logger.LogInformation(
					"Отправка email-уведомления: RecipientEmail={RecipientEmail}, DocumentTitle={DocumentTitle}",
					notification.RecipientEmail,
					notification.DocumentTitle);

				var sent = await SendNotificationMessage(notification);
				if (!sent)
				{
					_logger.LogWarning(
						"Email-уведомление не было отправлено: RecipientEmail={RecipientEmail}, DocumentTitle={DocumentTitle}",
						notification.RecipientEmail,
						notification.DocumentTitle);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Ошибка при обработке email-уведомления: RecipientEmail={RecipientEmail}, DocumentTitle={DocumentTitle}",
					notification.RecipientEmail,
					notification.DocumentTitle);
				throw;
			}
		}

		public async Task<bool> SendNotificationMessage(NotificationMessage notification)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.MailLogin));
				message.To.Add(new MailboxAddress(notification.RecipientName, notification.RecipientEmail));
				message.Subject = "Вам пришел новый документ на подпись";

				var bodyBuilder = new BodyBuilder
				{
					TextBody =
						$"Добрый день, {notification.RecipientName}.\n\n" +
						$"Документ \"{notification.DocumentTitle}\" доступен для подписания в личном кабинете.\n" +
						$"Инициатор: {notification.RequestedByName}.\n" +
						$"Дата запроса: {notification.RequestedAt:dd.MM.yyyy HH:mm}."
				};
				message.Body = bodyBuilder.ToMessageBody();

				using var client = new MailKit.Net.Smtp.SmtpClient();
				var socketOptions = _emailSettings.SmtpClientPort == 465
					? SecureSocketOptions.SslOnConnect
					: SecureSocketOptions.StartTls;

				await client.ConnectAsync(
					_emailSettings.SmtpClientHost,
					_emailSettings.SmtpClientPort,
					socketOptions);
				await client.AuthenticateAsync(
					_emailSettings.MailLogin,
					_emailSettings.MailPassword);
				await client.SendAsync(message);
				await client.DisconnectAsync(true);

				_logger.LogInformation(
					"Email-уведомление отправлено: RecipientEmail={RecipientEmail}, DocumentTitle={DocumentTitle}",
					notification.RecipientEmail,
					notification.DocumentTitle);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Ошибка отправки email-уведомления: RecipientEmail={RecipientEmail}, DocumentTitle={DocumentTitle}",
					notification.RecipientEmail,
					notification.DocumentTitle);
				return false;
			}
		}
	}
}
