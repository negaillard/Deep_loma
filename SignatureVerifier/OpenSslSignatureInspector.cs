using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SignatureVerifier;

internal static class OpenSslSignatureInspector
{
    /// <summary>
    /// При успешном smime -verify содержимое подписанного файла идёт в stdout (для PDF это «мусор»).
    /// Отправляем в NUL /dev/null, оставляем только stderr с «Verification successful».
    /// </summary>
    private static string NullDevicePath =>
        OperatingSystem.IsWindows() ? "NUL" : "/dev/null";

    private static readonly Regex PemCertBlock = new(
        @"-----BEGIN CERTIFICATE-----[\s\S]*?-----END CERTIFICATE-----",
        RegexOptions.Compiled);

    internal static string? TryResolveOpenSslPath()
    {
        var env = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(env))
        {
            foreach (var dir in env.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(dir.Trim('"'), "openssl.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        string[] common =
        [
            @"C:\Program Files\OpenSSL-Win64\bin\openssl.exe",
            @"C:\Program Files (x86)\OpenSSL-Win64\bin\openssl.exe",
            @"C:\OpenSSL-Win64\bin\openssl.exe",
        ];

        foreach (var p in common)
        {
            if (File.Exists(p))
                return p;
        }

        return null;
    }

    internal static ProcessResult Run(string opensslExe, IReadOnlyList<string> arguments, string? standardInput = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = opensslExe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput != null,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in arguments)
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi);
        if (p == null)
            return new ProcessResult(-1, "", "Не удалось запустить процесс OpenSSL.");

        if (standardInput != null)
        {
            p.StandardInput.Write(standardInput);
            p.StandardInput.Close();
        }

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return new ProcessResult(p.ExitCode, stdout, stderr);
    }

    internal static string BuildReport(string opensslExe, string signaturePath, string documentPath)
    {
        var sb = new StringBuilder();

        void AppendSection(string title, string body)
        {
            sb.AppendLine(new string('=', 60));
            sb.AppendLine(title);
            sb.AppendLine(new string('=', 60));
            sb.AppendLine(string.IsNullOrWhiteSpace(body) ? "(пусто)" : body.TrimEnd());
            sb.AppendLine();
        }

        // 1) Проверка подписи; -out NUL отбрасывает извлечённый документ (иначе в stdout попадает бинарный PDF)
        var verify = Run(opensslExe,
        [
            "smime", "-verify", "-inform", "DER",
            "-in", signaturePath,
            "-content", documentPath,
            "-noverify",
            "-out", NullDevicePath,
        ]);
        var verifyCombined = verify.StdOut + verify.StdErr;
        AppendSection("Результат smime -verify", verifyCombined);

        if (verifyCombined.Contains("Verification successful", StringComparison.OrdinalIgnoreCase))
            AppendSection("Итог", "Подпись верна (Verification successful).");
        else
            AppendSection("Итог", "Проверка не подтвердила подпись или вывод не содержит \"Verification successful\".");

        // 2) Человекочитаемая структура PKCS#7
        var pkcs7Print = Run(opensslExe, ["pkcs7", "-inform", "DER", "-in", signaturePath, "-noout", "-print"]);
        if (pkcs7Print.ExitCode == 0)
            AppendSection("Структура PKCS#7 (pkcs7 -print)", pkcs7Print.StdOut + pkcs7Print.StdErr);
        else
            AppendSection("Структура PKCS#7 (pkcs7 -print)", $"Код выхода: {pkcs7Print.ExitCode}\n{pkcs7Print.StdOut}{pkcs7Print.StdErr}");

        // 3) Сертификаты подписанта(ов) и цепочка
        var printCerts = Run(opensslExe, ["pkcs7", "-inform", "DER", "-in", signaturePath, "-print_certs"]);
        var certsPem = printCerts.StdOut;
        AppendSection("Сертификаты из контейнера (pkcs7 -print_certs, PEM)", certsPem + printCerts.StdErr);

        var matches = PemCertBlock.Matches(certsPem);
        if (matches.Count == 0)
        {
            AppendSection("Детали сертификатов (x509 -text)",
                "В выводе не найдено блоков PEM — возможно, формат не PKCS#7 или другая ошибка OpenSSL.");
        }
        else
        {
            for (var i = 0; i < matches.Count; i++)
            {
                var pem = matches[i].Value;
                var x509 = Run(opensslExe, ["x509", "-text", "-noout", "-nameopt", "multiline", "-utf8"], pem);
                var title = $"Сертификат #{i + 1} из {matches.Count} (openssl x509 -text)";
                AppendSection(title, x509.StdOut + x509.StdErr);
            }
        }

        // 4) Доп. ASN.1 дамп (метаданные в сыром виде)
        var asn1 = Run(opensslExe, ["asn1parse", "-inform", "DER", "-i", "-in", signaturePath]);
        AppendSection("ASN.1 (asn1parse -inform DER -i)", asn1.StdOut + asn1.StdErr);

        return sb.ToString();
    }

    internal readonly record struct ProcessResult(int ExitCode, string StdOut, string StdErr);
}
