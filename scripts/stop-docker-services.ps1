[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$IncludeDb
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeArgs = @(
    "compose",
    "-f", "docker-compose.yml",
    "-f", "docker-compose.local.yml",
    "stop",
    "apigateway",
    "authservice",
    "customerservice",
    "orderservice",
    "productservice",
    "forecastservice",
    "predictionservice",
    "analyticsservice",
    "adminservice"
)

if ($IncludeDb) {
    $composeArgs += "sqlserver"
}

function Invoke-RepoCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [string[]]$ArgumentList = @(),

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    Push-Location $repoRoot
    try {
        & $FilePath @ArgumentList
        if ($LASTEXITCODE -ne 0) {
            throw $FailureMessage
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  InsightERP - Docker Local Stop"            -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($IncludeDb) {
    Write-Host "Stopping application containers and sqlserver..." -ForegroundColor Yellow
}
else {
    Write-Host "Stopping application containers..." -ForegroundColor Yellow
}

if ($PSCmdlet.ShouldProcess(("docker " + ($composeArgs -join " ")), "Run compose")) {
    Invoke-RepoCommand -FilePath "docker" -ArgumentList $composeArgs -FailureMessage "Failed to stop one or more Docker-local services."
}

Write-Host ""
Write-Host "Stop request completed." -ForegroundColor Green
if (-not $IncludeDb) {
    Write-Host "sqlserver is still running. Use .\scripts\stop-docker-services.ps1 -IncludeDb to stop it as well." -ForegroundColor DarkGray
}
Write-Host ""
