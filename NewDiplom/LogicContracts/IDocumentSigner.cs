using Contracts.ViewModels;

namespace Contracts.LogicContracts
{
	/// <summary>
	/// Абстракция над алгоритмом криптографического подписания документа.
	/// Внутренний режим: RSA-2048 / SHA-256 / PKCS#7 (реализация через встроенный .NET).
	/// Юридически значимый режим: ГОСТ Р 34.10-2012 / ГОСТ Р 34.11-2012 (реализация через КриптоПро CSP).
	/// </summary>
	public interface IDocumentSigner
	{
		/// <summary>
		/// Подписывает байты документа с использованием ключа из переданного сертификата.
		/// Возвращает байты подписи (detached — без тела документа внутри).
		/// </summary>
		Task<byte[]> SignAsync(byte[] documentBytes, CertificateViewModel certificate);
	}
}
