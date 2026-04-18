[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string[]]$Services = @(),
    [switch]$SkipDbSetup
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$global:LASTEXITCODE = 0

$repoRoot = Split-Path -Parent $PSScriptRoot
$dbSetupScript = Join-Path $PSScriptRoot "setup-local-db.ps1"
$composeProjectName = "erp_backend"
$composeProjectPattern = "^[0-9a-f]+_{0}-" -f [regex]::Escape($composeProjectName)
$composeFileArgs = @("-f", "docker-compose.yml", "-f", "docker-compose.local.yml")
$composePrefix = @("compose", "-p", $composeProjectName) + $composeFileArgs
$serviceDefinitions = @(
    [pscustomobject]@{ Name = "apigateway";        DisplayName = "ApiGateway";         Aliases = @("apigateway", "api-gateway", "gateway");             SwaggerPath = $null;               HealthPath = "/health";             DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "authservice";       DisplayName = "AuthService";        Aliases = @("authservice", "auth-service", "auth");             SwaggerPath = "/auth/swagger";     HealthPath = "/auth/health";        DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "customerservice";   DisplayName = "CustomerService";    Aliases = @("customerservice", "customer-service", "customer"); SwaggerPath = "/swagger";          HealthPath = "/health";             DirectBaseUrl = "http://localhost:5002" },
    [pscustomobject]@{ Name = "orderservice";      DisplayName = "OrderService";       Aliases = @("orderservice", "order-service", "order");         SwaggerPath = "/order/swagger";    HealthPath = "/order/health";       DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "productservice";    DisplayName = "ProductService";     Aliases = @("productservice", "product-service", "product");   SwaggerPath = "/product/swagger";  HealthPath = "/product/health";     DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "forecastservice";   DisplayName = "ForecastService";    Aliases = @("forecastservice", "forecast-service", "forecast"); SwaggerPath = "/forecast/swagger"; HealthPath = "/forecast/health";    DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "predictionservice"; DisplayName = "PredictionService";  Aliases = @("predictionservice", "prediction-service", "prediction"); SwaggerPath = "/prediction/swagger"; HealthPath = "/prediction/health"; DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "analyticsservice";  DisplayName = "AnalyticsService";   Aliases = @("analyticsservice", "analytics-service", "analytics"); SwaggerPath = "/analytics/swagger"; HealthPath = "/analytics/health";  DirectBaseUrl = $null },
    [pscustomobject]@{ Name = "adminservice";      DisplayName = "AdminService";       Aliases = @("adminservice", "admin-service", "admin");         SwaggerPath = "/admin/swagger";    HealthPath = "/admin/health";       DirectBaseUrl = $null }
)

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-CommandAvailable {
    param([string]$CommandName)

    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Test-NativeCommandSucceeds {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [string[]]$ArgumentList = @()
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $exitCode = 1

    try {
        $ErrorActionPreference = "Continue"
        & $FilePath @ArgumentList > $null 2>$null
        $exitCode = $global:LASTEXITCODE
    }
    catch {
        return $false
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return $exitCode -eq 0
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

function Invoke-PowerShellScript {
    param(
        [Parameter(Mandatory)]
        [string]$ScriptPath,

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    $powerShellCommand = Get-Command "powershell.exe" -ErrorAction SilentlyContinue
    if ($null -eq $powerShellCommand) {
        throw "Windows PowerShell was not found. Run this script from Windows PowerShell."
    }

    $process = Start-Process -FilePath $powerShellCommand.Source `
        -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath) `
        -Wait `
        -PassThru `
        -NoNewWindow

    if ($process.ExitCode -ne 0) {
        throw $FailureMessage
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
        throw "Failed to inspect Docker containers before startup."
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

function Resolve-RequestedServices {
    param([string[]]$RequestedServices)

    if ($RequestedServices.Count -eq 0) {
        return @($serviceDefinitions.Name)
    }

    $aliasMap = @{}
    foreach ($service in $serviceDefinitions) {
        foreach ($alias in ($service.Aliases + $service.Name + $service.DisplayName)) {
            $aliasMap[$alias.ToLowerInvariant()] = $service.Name
        }
    }

    $selected = @{}
    foreach ($entry in $RequestedServices) {
        if ($null -eq $entry) {
            continue
        }

        foreach ($candidate in ($entry -split ",")) {
            $trimmed = $candidate.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }

            $lookupKey = $trimmed.ToLowerInvariant()
            if (-not $aliasMap.ContainsKey($lookupKey)) {
                $validNames = ($serviceDefinitions.DisplayName -join ", ")
                throw "Unknown service '$trimmed'. Valid values: $validNames"
            }

            $selected[$aliasMap[$lookupKey]] = $true
        }
    }

    if ($selected.Count -eq 0) {
        throw "No valid services were specified."
    }

    $orderedSelection = New-Object System.Collections.Generic.List[string]
    if ($selected.ContainsKey("apigateway")) {
        $orderedSelection.Add("apigateway")
    }

    foreach ($service in $serviceDefinitions) {
        if ($service.Name -eq "apigateway") {
            continue
        }

        if ($selected.ContainsKey($service.Name)) {
            $orderedSelection.Add($service.Name)
        }
    }

    return @($orderedSelection)
}

$selectedServices = Resolve-RequestedServices -RequestedServices $Services
$selectedServiceDefinitions = foreach ($serviceName in $selectedServices) {
    $serviceDefinitions | Where-Object { $_.Name -eq $serviceName }
}
$servicesWithoutGateway = @($selectedServices | Where-Object { $_ -ne "apigateway" })

Write-Step "Validating Docker tooling"
if (-not (Test-CommandAvailable "docker")) {
    throw "Docker CLI was not found. Install Docker Desktop, open it, and try again."
}

if (-not (Test-NativeCommandSucceeds -FilePath "docker" -ArgumentList @("info"))) {
    throw "Docker is installed but the daemon is not available. Start Docker Desktop and try again."
}

if (-not (Test-NativeCommandSucceeds -FilePath "docker" -ArgumentList @("compose", "version"))) {
    throw "Docker Compose v2 is not available. Update Docker Desktop and try again."
}

Write-Step "Selected Docker services"
foreach ($service in $selectedServiceDefinitions) {
    Write-Host "  $($service.DisplayName) -> $($service.Name)" -ForegroundColor White
}

if (-not $SkipDbSetup) {
    if (-not (Test-Path $dbSetupScript)) {
        throw "Database setup script not found: $dbSetupScript"
    }

    Write-Step "Preparing local SQL Server"
    if ($PSCmdlet.ShouldProcess($dbSetupScript, "Run database setup")) {
        Invoke-PowerShellScript -ScriptPath $dbSetupScript -FailureMessage "Database setup failed."
    }
}
else {
    Write-Step "Skipping database setup"
    Write-Host "  Reusing the existing sqlserver container and schema state." -ForegroundColor DarkGray
}

Remove-RenamedComposeContainers

if ($selectedServices -contains "apigateway") {
    $gatewayArgs = $composePrefix + @("up", "-d", "--build", "--remove-orphans", "apigateway")
    Write-Step "Starting ApiGateway"
    if ($PSCmdlet.ShouldProcess(("docker " + ($gatewayArgs -join " ")), "Run compose")) {
        Invoke-RepoCommand -FilePath "docker" -ArgumentList $gatewayArgs -FailureMessage "Failed to start ApiGateway with docker compose."
    }
}

if ($servicesWithoutGateway.Count -gt 0) {
    $serviceArgs = $composePrefix + @("up", "-d", "--build", "--remove-orphans") + $servicesWithoutGateway
    Write-Step "Starting selected microservices"
    if ($PSCmdlet.ShouldProcess(("docker " + ($serviceArgs -join " ")), "Run compose")) {
        Invoke-RepoCommand -FilePath "docker" -ArgumentList $serviceArgs -FailureMessage "Failed to start one or more selected microservices with docker compose."
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  InsightERP - Docker Local Startup"         -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($selectedServices -contains "apigateway") {
    Write-Host "  Gateway:" -ForegroundColor Yellow
    Write-Host "    Base URL   http://localhost:5000" -ForegroundColor White
    Write-Host "    Health     http://localhost:5000/health" -ForegroundColor White
    Write-Host ""
    Write-Host "  Gateway Swagger entrypoints:" -ForegroundColor Yellow

    foreach ($service in $selectedServiceDefinitions) {
        if ($null -ne $service.SwaggerPath) {
            if ($service.Name -eq "customerservice") {
                Write-Host ("    {0,-20} {1}{2} (direct)" -f $service.DisplayName, $service.DirectBaseUrl, $service.SwaggerPath) -ForegroundColor White
            }
            else {
                Write-Host ("    {0,-20} http://localhost:5000{1}" -f $service.DisplayName, $service.SwaggerPath) -ForegroundColor White
            }
        }
    }

    Write-Host ""
    Write-Host "  Gateway health checks:" -ForegroundColor Yellow
    foreach ($service in $selectedServiceDefinitions) {
        if ($service.Name -ne "apigateway") {
            if ($service.Name -eq "customerservice") {
                Write-Host ("    {0,-20} {1}{2} (direct)" -f $service.DisplayName, $service.DirectBaseUrl, $service.HealthPath) -ForegroundColor White
            }
            else {
                Write-Host ("    {0,-20} http://localhost:5000{1}" -f $service.DisplayName, $service.HealthPath) -ForegroundColor White
            }
        }
    }
}
else {
    Write-Host "  ApiGateway was not selected." -ForegroundColor Yellow
    if ($selectedServices -contains "customerservice") {
        Write-Host "  CustomerService is exposed directly even without ApiGateway." -ForegroundColor Yellow
        Write-Host "    Base URL   http://localhost:5002" -ForegroundColor White
        Write-Host "    Health     http://localhost:5002/health" -ForegroundColor White
        Write-Host "    Swagger    http://localhost:5002/swagger" -ForegroundColor White
    }

    if (@($servicesWithoutGateway | Where-Object { $_ -ne "customerservice" }).Count -gt 0) {
        Write-Host "  The remaining started microservices are internal-only containers and are not reachable from the host without ApiGateway." -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "  Stop command:" -ForegroundColor Yellow
Write-Host "    .\scripts\stop-docker-services.ps1" -ForegroundColor White
Write-Host ""
