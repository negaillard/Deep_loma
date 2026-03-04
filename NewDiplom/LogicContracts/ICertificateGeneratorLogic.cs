using Contracts.BindingModels;

namespace Contracts.LogicContracts
{
	/// <summary>
	/// Генерирует сертификаты для внутреннего режима работы системы.
	/// </summary>
	public interface ICertificateGeneratorLogic
	{
		/// <summary>
		/// Создаёт самоподписанный сертификат RSA-2048 / SHA-256.
		/// Возвращает заполненный BindingModel с публичным ключом в PEM-формате
		/// и путём до сохранённого PFX-файла.
		/// </summary>
		Task<CertificateBindingModel> GenerateSelfSignedAsync(int userId, string owner, string publisher);
	}
}
