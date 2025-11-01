<#
  test-db-connection.ps1

  Simple PowerShell helper to test a SQL Server connection using environment
  variables. DO NOT commit real secrets to source control. This script reads
  the following env vars:

    RDS_HOST, RDS_PORT, RDS_DB, RDS_USER, RDS_PASSWORD

  Usage (example):

    # set env vars in your shell (temporary for the session)
    $env:RDS_HOST='168.119.153.239'
    $env:RDS_PORT='1433'
    $env:RDS_DB='ECommerceDb'
    $env:RDS_USER='website'
    $env:RDS_PASSWORD='web4814!?'

    # run the test
    pwsh -File .\scripts\test-db-connection.ps1

  The script will attempt to open a SqlConnection and print a short result.
#>

param(
  [string]$HostEnv = 'RDS_HOST',
  [string]$PortEnv = 'RDS_PORT',
  [string]$DbEnv = 'RDS_DB',
  [string]$UserEnv = 'RDS_USER',
  [string]$PassEnv = 'RDS_PASSWORD',
  [int]$DefaultPort = 1433,
  [switch]$NoEncrypt
)

Write-Host "Starting DB connection test (reads env vars or use params)" -ForegroundColor Cyan

function Get-EnvOrFail($name, $friendly) {
  $v = [System.Environment]::GetEnvironmentVariable($name)
  if ([string]::IsNullOrWhiteSpace($v)) {
    Write-Error "Environment variable '$name' ($friendly) is not set. Aborting."
    exit 2
  }
  return $v
}

# Use non-reserved variable names to avoid colliding with PowerShell automatic vars (Host, etc.)
$rdsHost = Get-EnvOrFail $HostEnv 'host'
$rdsPort = [System.Environment]::GetEnvironmentVariable($PortEnv)
if ([string]::IsNullOrWhiteSpace($rdsPort)) { $rdsPort = $DefaultPort }
$rdsDb = Get-EnvOrFail $DbEnv 'database'
$rdsUser = Get-EnvOrFail $UserEnv 'user'
$rdsPass = Get-EnvOrFail $PassEnv 'password'

$encrypt = if ($NoEncrypt) { 'False' } else { 'True' }

$cs = "Server=$rdsHost,$rdsPort;Initial Catalog=$rdsDb;User ID=$rdsUser;Password=$rdsPass;Encrypt=$encrypt;TrustServerCertificate=True;Connection Timeout=8;"

Write-Host "Attempting connection to ${rdsHost}:${rdsPort} (database: ${rdsDb}, user: ${rdsUser}, Encrypt=${encrypt})" -ForegroundColor Yellow

try {
  $conn = New-Object System.Data.SqlClient.SqlConnection $cs
  $conn.Open()
  Write-Host 'SQL OPEN OK' -ForegroundColor Green
  $conn.Close()
  exit 0
} catch {
  Write-Host "SQL FAILED: $($_.Exception.Message)" -ForegroundColor Red
  if ($_.Exception.InnerException) { Write-Host "INNER: $($_.Exception.InnerException.Message)" -ForegroundColor Red }
  exit 3
}
