# Требуется OpenSSL в PATH (или укажите полный путь к openssl.exe через -OpenSslExe).
# Windows PowerShell 5.1 и PowerShell 7+.
param(
    [Parameter(Mandatory = $false)]
    [string] $SignaturePath = "",

    [Parameter(Mandatory = $false)]
    [string] $DocumentPath = "",

    [Parameter(Mandatory = $false)]
    [string] $OpenSslExe = "openssl"
)

$ErrorActionPreference = "Stop"

function Escape-Arg([string] $s) {
    if ($s -match '[\s"]') {
        '"' + ($s -replace '"', '\"') + '"'
    }
    else {
        $s
    }
}

function Invoke-OpenSsl {
    param([string[]] $OpenSslArgs, [string] $StdIn = $null)

    $argLine = ($OpenSslArgs | ForEach-Object { Escape-Arg $_ }) -join ' '

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $OpenSslExe
    $psi.Arguments = $argLine
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardInput = ($null -ne $StdIn)
    $psi.CreateNoWindow = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8

    $p = [System.Diagnostics.Process]::Start($psi)
    if ($StdIn) {
        $p.StandardInput.Write($StdIn)
        $p.StandardInput.Close()
    }
    $out = $p.StandardOutput.ReadToEnd()
    $err = $p.StandardError.ReadToEnd()
    $p.WaitForExit()
    return @{ ExitCode = $p.ExitCode; StdOut = $out; StdErr = $err }
}

try {
    if (-not $SignaturePath -or -not $DocumentPath) {
        Write-Host "Ошибка: укажите -SignaturePath и -DocumentPath." -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path -LiteralPath $SignaturePath)) {
        Write-Host "Ошибка: файл подписи не найден." -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path -LiteralPath $DocumentPath)) {
        Write-Host "Ошибка: файл документа не найден." -ForegroundColor Red
        exit 1
    }

    # Без -out в stdout попадает сам документ (PDF) — выглядит как «мусор». Пишем в NUL.
    $nullDevice = if ([Environment]::OSVersion.Platform -eq 'Win32NT') { 'NUL' } else { '/dev/null' }

    $verify = Invoke-OpenSsl -OpenSslArgs @(
        "smime", "-verify", "-inform", "DER",
        "-in", $SignaturePath,
        "-content", $DocumentPath,
        "-noverify",
        "-out", $nullDevice
    )
    $verifyText = $verify.StdOut + $verify.StdErr

    Write-Host "======== smime -verify ========"
    Write-Host $verifyText

    if ($verifyText -match "Verification successful") {
        Write-Host "Подпись верна." -ForegroundColor Green
    }
    else {
        Write-Host "Проверка не подтвердила подпись (нет строки Verification successful)." -ForegroundColor Yellow
    }

    Write-Host "`n======== Структура PKCS#7 (pkcs7 -print) ========"
    $p7 = Invoke-OpenSsl -OpenSslArgs @("pkcs7", "-inform", "DER", "-in", $SignaturePath, "-noout", "-print")
    Write-Host ($p7.StdOut + $p7.StdErr)

    Write-Host "`n======== Сертификаты (pkcs7 -print_certs) ========"
    $certsOut = Invoke-OpenSsl -OpenSslArgs @("pkcs7", "-inform", "DER", "-in", $SignaturePath, "-print_certs")
    $pemBundle = $certsOut.StdOut
    Write-Host ($pemBundle + $certsOut.StdErr)

    Write-Host "`n======== Детали сертификатов (x509 -text) ========"
    $blocks = [regex]::Matches($pemBundle, "(?s)-----BEGIN CERTIFICATE-----.+?-----END CERTIFICATE-----")
    $i = 1
    foreach ($m in $blocks) {
        Write-Host "--- Сертификат $i из $($blocks.Count) ---"
        $x = Invoke-OpenSsl -OpenSslArgs @("x509", "-text", "-noout", "-nameopt", "multiline", "-utf8") -StdIn $m.Value
        Write-Host ($x.StdOut + $x.StdErr)
        $i++
    }
    if ($blocks.Count -eq 0) {
        Write-Host "PEM-блоки не найдены."
    }

    Write-Host "`n======== ASN.1 (asn1parse) ========"
    $asn = Invoke-OpenSsl -OpenSslArgs @("asn1parse", "-inform", "DER", "-i", "-in", $SignaturePath)
    Write-Host ($asn.StdOut + $asn.StdErr)
}
catch {
    Write-Host "Ошибка: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host $_.Exception.InnerException.Message -ForegroundColor Red
    }
    exit 1
}
