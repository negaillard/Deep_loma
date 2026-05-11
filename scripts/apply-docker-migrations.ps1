<#
  Applies EF Core migrations to Docker Compose SQL (DCP_Internal / DCP_Local).

  Requires:
  - Stack running (SQL ports 14333 internal, 14334 local) or at least the SQL container.
  - dotnet-ef: dotnet tool install --global dotnet-ef
  - .env in repo root with MSSQL_SA_PASSWORD (same as compose).

  Examples:
    .\scripts\apply-docker-migrations.cmd
    .\scripts\apply-docker-migrations.cmd -Target internal
    If ExecutionPolicy blocks .ps1, use .cmd

    Or for current PowerShell session only:
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
    .\scripts\apply-docker-migrations.ps1 -Target local
#>

param(
    [ValidateSet('internal', 'local', 'both')]
    [string] $Target = 'both'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$envFile = Join-Path $repoRoot '.env'
if (-not (Test-Path $envFile)) {
    Write-Error "Missing .env. Copy env.example to .env and set MSSQL_SA_PASSWORD."
}

Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]*)\s*=\s*(.*)\s*$') {
        $name = $matches[1].Trim()
        $value = $matches[2].Trim().Trim('"')
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    }
}

$pw = [Environment]::GetEnvironmentVariable('MSSQL_SA_PASSWORD', 'Process')
if ([string]::IsNullOrWhiteSpace($pw)) {
    Write-Error "MSSQL_SA_PASSWORD is not set in .env"
}

# Ports from docker-compose.internal.yml / docker-compose.local.yml
$portInternal = 14333
$portLocal = 14334

function Invoke-Migrate {
    param(
        [string] $Port,
        [string] $Database
    )

    $conn = "Server=localhost,$Port;Database=$Database;User Id=sa;Password=$pw;TrustServerCertificate=True;MultipleActiveResultSets=True"
    Write-Host ">>> $Database @ localhost,$Port" -ForegroundColor Cyan

    dotnet ef database update `
        --project Storage/Storage.csproj `
        --startup-project API/API.csproj `
        --context StorageContext `
        --connection $conn
}

if ($Target -eq 'internal' -or $Target -eq 'both') {
    Invoke-Migrate -Port $portInternal -Database 'DCP_Internal'
}

if ($Target -eq 'local' -or $Target -eq 'both') {
    Invoke-Migrate -Port $portLocal -Database 'DCP_Local'
}

Write-Host "Done." -ForegroundColor Green
