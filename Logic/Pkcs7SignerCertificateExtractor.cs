using System.Diagnostics.CodeAnalysis;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;

namespace Logic;

/// <summary>
/// Извлечение сертификата подписанта из CMS/PKCS#7. Для ГОСТ <see cref="System.Security.Cryptography.Pkcs.SignedCms"/>
/// часто не заполняет коллекцию сертификатов и <c>SignerInfo.Certificate</c>, хотя в ASN.1 они присутствуют.
/// </summary>
public static class Pkcs7SignerCertificateExtractor
{
	/// <summary>
	/// Возвращает DER сертификата подписанта (сначала по SignerID, иначе первый из набора в SignedData).
	/// </summary>
	public static bool TryGetSignerCertificateDer(
		byte[] pkcs7Der,
		[NotNullWhen(true)] out byte[]? certDer)
	{
		certDer = null;
		try
		{
			var cms = new CmsSignedData(pkcs7Der);
			var certStore = cms.GetCertificates();
			var signerInfos = cms.GetSignerInfos();

			foreach (SignerInformation signer in signerInfos.GetSigners())
			{
				foreach (X509Certificate cert in certStore.EnumerateMatches(signer.SignerID))
				{
					certDer = cert.GetEncoded();
					return true;
				}
			}

			foreach (X509Certificate cert in certStore.EnumerateMatches(new X509CertStoreSelector()))
			{
				certDer = cert.GetEncoded();
				return true;
			}
		}
		catch
		{
			return false;
		}

		return false;
	}
}
