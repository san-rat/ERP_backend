param(
    [string]$ContainerName = "erp-sqlserver-local",
    [string]$DatabaseName = "insighterp_db",
    [string]$SaPassword = "LocalDev_Password123!",
    [string]$MigrationsPath = "schemas/auth/migrations",
    [int]$StartupTimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$normalizedMigrationsPath = $MigrationsPath.Replace("\", "/").TrimStart('.', '/')
$pathParts = $normalizedMigrationsPath -split "/"

if ($pathParts.Length -lt 3 -or $pathParts[0] -ne "schemas") {
    throw "MigrationsPath must look like 'schemas/<schema>/migrations'. Received '$MigrationsPath'."
}

$schemaName = $pathParts[1]
$localMigrationsPath = Join-Path $repoRoot ($normalizedMigrationsPath -replace "/", [System.IO.Path]::DirectorySeparatorChar)
$localMigrationRunner = Join-Path $repoRoot "scripts/apply_sqlserver_migrations.sh"

if (-not (Test-Path $localMigrationsPath)) {
    throw "Migrations folder not found: $localMigrationsPath"
}

if (-not (Test-Path $localMigrationRunner)) {
    throw "Migration runner not found: $localMigrationRunner"
}

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [string[]]$ArgumentList = @(),

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Test-CommandAvailable {
    param([string]$CommandName)

    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Test-SqlServerReady {
    param(
        [string]$Container,
        [string]$Password,
        [string[]]$SqlCmdCandidates
    )

    foreach ($candidate in $SqlCmdCandidates) {
        & docker exec $Container $candidate -S localhost -U sa -P $Password -C -Q "SET NOCOUNT ON; SELECT 1;" > $null 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $candidate
        }
    }

    return $null
}

if (-not (Test-CommandAvailable "docker")) {
    throw "Docker CLI was not found. Install Docker Desktop, open it, and try again."
}

& docker info > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Docker is installed but the daemon is not available. Start Docker Desktop and try again."
}

& docker compose version > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose v2 is not available. Update Docker Desktop and try again."
}

Write-Step "Starting the local SQL Server container"
Push-Location $repoRoot
try {
    Invoke-NativeCommand -FilePath "docker" -ArgumentList @("compose", "up", "-d", "sqlserver") -FailureMessage "Failed to start the sqlserver service with docker compose."
}
finally {
    Pop-Location
}

$sqlCmdCandidates = @(
    "/opt/mssql-tools18/bin/sqlcmd",
    "/opt/mssql-tools/bin/sqlcmd"
)

Write-Step "Waiting for SQL Server to accept connections"
$deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
$sqlCmdPath = $null

while ((Get-Date) -lt $deadline) {
    $sqlCmdPath = Test-SqlServerReady -Container $ContainerName -Password $SaPassword -SqlCmdCandidates $sqlCmdCandidates
    if ($sqlCmdPath) {
        break
    }

    Start-Sleep -Seconds 2
}

if (-not $sqlCmdPath) {
    throw "SQL Server did not become ready within $StartupTimeoutSeconds seconds, or sqlcmd is unavailable inside '$ContainerName'."
}

Write-Step "Ensuring database '$DatabaseName' exists"
$createDatabaseQuery = "IF DB_ID(N'$DatabaseName') IS NULL BEGIN CREATE DATABASE [$DatabaseName]; END;"
Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "exec",
    $ContainerName,
    $sqlCmdPath,
    "-S", "localhost",
    "-U", "sa",
    "-P", $SaPassword,
    "-C",
    "-Q", $createDatabaseQuery
) -FailureMessage "Failed to create or verify database '$DatabaseName'."

$containerWorkDir = "/tmp/local-db-setup-$([Guid]::NewGuid().ToString('N'))"
$containerSchemaParent = "$containerWorkDir/schemas/$schemaName"
$containerCommand = "cd '$containerWorkDir' && bash ./apply_sqlserver_migrations.sh $normalizedMigrationsPath"

Write-Step "Copying migration runner and '$schemaName' migrations into the container"
Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "exec",
    $ContainerName,
    "/bin/bash",
    "-lc",
    "mkdir -p '$containerSchemaParent'"
) -FailureMessage "Failed to prepare a temporary workspace inside '$ContainerName'."

Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "cp",
    $localMigrationRunner,
    "${ContainerName}:${containerWorkDir}/apply_sqlserver_migrations.sh"
) -FailureMessage "Failed to copy the migration runner into '$ContainerName'."

Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "cp",
    $localMigrationsPath,
    "${ContainerName}:${containerSchemaParent}/"
) -FailureMessage "Failed to copy migrations into '$ContainerName'."

Write-Step "Applying migrations for schema '$schemaName'"
Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "exec",
    "-e", "AZURE_SQL_SERVER=localhost",
    "-e", "AZURE_SQL_DATABASE=$DatabaseName",
    "-e", "AZURE_SQL_USER=sa",
    "-e", "AZURE_SQL_PASSWORD=$SaPassword",
    $ContainerName,
    "/bin/bash",
    "-lc",
    $containerCommand
) -FailureMessage "Failed to apply migrations for schema '$schemaName'."

$migrationCount = & docker exec $ContainerName $sqlCmdPath -S localhost -U sa -P $SaPassword -C -d $DatabaseName -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM [$schemaName].schema_migrations;" 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Migrations ran, but the final verification query failed."
}

$appliedMigrations = ($migrationCount | Select-Object -Last 1).ToString().Trim()
$connectionString = "Server=localhost,1433;Database=$DatabaseName;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;"

Write-Host ""
Write-Host "Local database setup is complete." -ForegroundColor Green
Write-Host "Schema: $schemaName"
Write-Host "Applied migrations recorded in [$schemaName].schema_migrations: $appliedMigrations"
Write-Host ""
Write-Host "AuthService connection string:" -ForegroundColor Yellow
Write-Host $connectionString
Write-Host ""
Write-Host "Next step: run 'dotnet run --project src/AuthService' and test http://localhost:5001/health" -ForegroundColor DarkGray
