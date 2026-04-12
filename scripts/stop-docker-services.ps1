[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$IncludeDb
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$global:LASTEXITCODE = 0

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeProjectName = "erp_backend"
$composeProjectPattern = "^[0-9a-f]+_{0}-" -f [regex]::Escape($composeProjectName)
$composeArgs = @(
    "compose",
    "-p", $composeProjectName,
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

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
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
        $previousErrorActionPreference = $ErrorActionPreference
        $exitCode = 1

        try {
            $ErrorActionPreference = "Continue"
            & $FilePath @ArgumentList
            $exitCode = $global:LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }

        if ($exitCode -ne 0) {
            throw $FailureMessage
        }
    }
    finally {
        Pop-Location
    }
}

function Get-RenamedComposeContainerNames {
    $previousErrorActionPreference = $ErrorActionPreference
    $exitCode = 1

    try {
        $ErrorActionPreference = "Continue"
        $containerNames = & docker ps -a --format "{{.Names}}" 2>$null
        $exitCode = $global:LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        throw "Failed to inspect Docker containers during cleanup."
    }

    $renamedContainers = New-Object System.Collections.Generic.List[string]
    foreach ($containerName in $containerNames) {
        $trimmedName = $containerName.ToString().Trim()
        if (-not [string]::IsNullOrWhiteSpace($trimmedName) -and $trimmedName -match $composeProjectPattern) {
            $renamedContainers.Add($trimmedName)
        }
    }

    return @($renamedContainers)
}

function Remove-RenamedComposeContainers {
    $renamedContainers = @(Get-RenamedComposeContainerNames)
    if ($renamedContainers.Count -eq 0) {
        return
    }

    Write-Step "Removing stale Docker containers left behind by interrupted recreates"
    foreach ($containerName in $renamedContainers) {
        Write-Host "  Removing $containerName" -ForegroundColor DarkGray
        if ($PSCmdlet.ShouldProcess($containerName, "docker rm -f")) {
            Invoke-RepoCommand -FilePath "docker" -ArgumentList @("rm", "-f", $containerName) -FailureMessage "Failed to remove stale Docker container '$containerName'."
        }
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

Remove-RenamedComposeContainers

Write-Host ""
Write-Host "Stop request completed." -ForegroundColor Green
if (-not $IncludeDb) {
    Write-Host "sqlserver is still running. Use .\scripts\stop-docker-services.ps1 -IncludeDb to stop it as well." -ForegroundColor DarkGray
}
Write-Host ""
