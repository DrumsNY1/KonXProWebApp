<#
.SYNOPSIS
    Sets deploy-time environment variables in a published web.config.

.DESCRIPTION
    Used by the deploy workflows after `dotnet publish`, before Web Deploy. Makes the pipeline
    the single owner of per-environment settings, so the server's web.config is never edited by hand.

    Sets, on the <aspNetCore> element:
      - ASPNETCORE_ENVIRONMENT                     = -EnvironmentName
      - ConnectionStrings__db_9f8bee_konxdevConnection = value of the env var named by -ConnectionStringEnvVar
      - stdoutLogEnabled / stdoutLogFile           = on, .\logs\stdout (startup failures are otherwise invisible)

    The connection string is read from an environment variable rather than a parameter so the
    secret never appears on a command line or in the step's echoed script. It is never printed.

.EXAMPLE
    $env:KONX_DB_CONNECTION = '<from a GitHub secret>'
    ./set-webconfig-env.ps1 -WebConfigPath ./publish/web.config -EnvironmentName Staging
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $WebConfigPath,
    [Parameter(Mandatory)] [ValidateSet('Staging', 'Production')] [string] $EnvironmentName,
    [string] $ConnectionStringEnvVar = 'KONX_DB_CONNECTION'
)

$ErrorActionPreference = 'Stop'

$connectionString = [Environment]::GetEnvironmentVariable($ConnectionStringEnvVar)
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "Environment variable '$ConnectionStringEnvVar' is empty. Is the connection-string secret set on this GitHub environment?"
}
if ($connectionString -match 'PLACEHOLDER') {
    throw "Environment variable '$ConnectionStringEnvVar' still contains 'PLACEHOLDER'. Set the real value in the GitHub environment secret."
}

if (-not (Test-Path $WebConfigPath)) {
    throw "web.config not found at '$WebConfigPath'. Did dotnet publish run?"
}

[xml] $xml = Get-Content -Path $WebConfigPath -Raw

$aspNetCore = $xml.SelectSingleNode('//system.webServer/aspNetCore')
if ($null -eq $aspNetCore) {
    throw "No <aspNetCore> element found in '$WebConfigPath'."
}

$aspNetCore.SetAttribute('stdoutLogEnabled', 'true')
$aspNetCore.SetAttribute('stdoutLogFile', '.\logs\stdout')

$envVars = $aspNetCore.SelectSingleNode('environmentVariables')
if ($null -eq $envVars) {
    $envVars = $xml.CreateElement('environmentVariables')
    [void] $aspNetCore.AppendChild($envVars)
}

function Set-EnvVar([System.Xml.XmlElement] $parent, [string] $name, [string] $value) {
    $existing = $parent.SelectNodes('environmentVariable') | Where-Object { $_.GetAttribute('name') -eq $name }
    foreach ($node in @($existing)) { [void] $parent.RemoveChild($node) }

    $node = $parent.OwnerDocument.CreateElement('environmentVariable')
    $node.SetAttribute('name', $name)
    $node.SetAttribute('value', $value)
    [void] $parent.AppendChild($node)
}

Set-EnvVar $envVars 'ASPNETCORE_ENVIRONMENT' $EnvironmentName
Set-EnvVar $envVars 'ConnectionStrings__db_9f8bee_konxdevConnection' $connectionString

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = New-Object System.Text.UTF8Encoding($false)
$writer = [System.Xml.XmlWriter]::Create((Resolve-Path $WebConfigPath).Path, $settings)
try { $xml.Save($writer) } finally { $writer.Dispose() }

$names = ($envVars.SelectNodes('environmentVariable') | ForEach-Object { $_.GetAttribute('name') }) -join ', '
Write-Host "web.config configured for '$EnvironmentName'. Environment variables set: $names (values not shown)."
