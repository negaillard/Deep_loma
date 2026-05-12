using Contracts.BindingModels;
using Contracts.LogicContracts;

namespace Logic.CertificateGenerators
{
	/// <summary>
	/// Режим Local: сервер не выпускает сертификаты. Нужен только чтобы удовлетворить DI для <see cref="CertificateLogic"/>.
	/// </summary>
	public sealed class LocalModeCertificateGeneratorStub : ICertificateGeneratorLogic
	{
		public Task<CertificateBindingModel> GenerateSelfSignedAsync(int userId, string owner, string publisher)
			=> throw new NotSupportedException(
				"Генерация сертификатов на сервере недоступна в режиме Local (подпись выполняется на клиенте).");
	}
}
