namespace Auth
{
	/// <summary>
	/// Опции только для тестов/CI. Если <see cref="TestBypassCode"/> задан,
	/// отправка кода не дергает SMTP, а verify-login принимает это значение без Redis.
	/// В production не задавать.
	/// </summary>
	public class AuthTestOptions
	{
		public string TestBypassCode { get; set; } = string.Empty;
	}
}
