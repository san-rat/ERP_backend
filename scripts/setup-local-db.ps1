param(
    [string]$ContainerName = "erp-sqlserver-local",
    [string]$DatabaseName = "insighterp_db",
    [string]$SaPassword = "LocalDev_Password123!",
    [string[]]$MigrationsPath = @(),
    [int]$StartupTimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$localSchemasRoot = Join-Path $repoRoot "schemas"
$localMigrationRunner = Join-Path $repoRoot "scripts/apply_sqlserver_migrations.sh"
$schemaOrder = @{
    auth       = 0
    customer   = 1
    product    = 2
    order      = 3
    prediction = 4
    analytics  = 5
}
$schemaNameOverrides = @{
    prediction = "ml"
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

function Normalize-MigrationsPath {
    param([Parameter(Mandatory)][string]$Path)

    $normalizedPath = $Path.Replace("\", "/").Trim()

    while ($normalizedPath.StartsWith("./")) {
        $normalizedPath = $normalizedPath.Substring(2)
    }

    $normalizedPath = $normalizedPath.TrimStart('/')
    $normalizedPath = $normalizedPath.TrimEnd('/')
    $pathParts = $normalizedPath -split "/"

    if ($pathParts.Length -ne 3 -or $pathParts[0] -ne "schemas" -or $pathParts[2] -ne "migrations") {
        throw "MigrationsPath must look like 'schemas/<schema>/migrations'. Received '$Path'."
    }

    return $normalizedPath
}

function Get-TargetSchemaName {
    param([Parameter(Mandatory)][string]$FolderName)

    if ($schemaNameOverrides.ContainsKey($FolderName)) {
        return $schemaNameOverrides[$FolderName]
    }

    return $FolderName
}

function Test-MigrationFileHasContent {
    param([Parameter(Mandatory)][string]$FilePath)

    $content = Get-Content -Path $FilePath -Raw
    return -not [string]::IsNullOrWhiteSpace($content)
}

function Get-MigrationTargets {
    param([string[]]$RequestedPaths)

    if (-not (Test-Path $localSchemasRoot)) {
        throw "Schemas folder not found: $localSchemasRoot"
    }

    $pathsToUse = @()

    if ($RequestedPaths.Count -gt 0) {
        $pathsToUse = $RequestedPaths
    }
    else {
        $pathsToUse = Get-ChildItem -Path $localSchemasRoot -Directory |
            ForEach-Object { "schemas/$($_.Name)/migrations" }
    }

    $seen = @{}
    $targets = New-Object System.Collections.Generic.List[object]

    foreach ($candidatePath in $pathsToUse) {
        $normalizedPath = Normalize-MigrationsPath -Path $candidatePath

        if ($seen.ContainsKey($normalizedPath)) {
            continue
        }

        $seen[$normalizedPath] = $true
        $pathParts = $normalizedPath -split "/"
        $folderName = $pathParts[1]
        $localPath = Join-Path $repoRoot ($normalizedPath -replace "/", [System.IO.Path]::DirectorySeparatorChar)

        if (-not (Test-Path $localPath)) {
            throw "Migrations folder not found: $localPath"
        }

        $migrationFiles = @(Get-ChildItem -Path $localPath -Filter *.sql -File | Sort-Object Name)
        $hasRunnableFiles = $false

        foreach ($migrationFile in $migrationFiles) {
            if (Test-MigrationFileHasContent -FilePath $migrationFile.FullName) {
                $hasRunnableFiles = $true
                break
            }
        }

        $sortWeight = if ($schemaOrder.ContainsKey($folderName)) { $schemaOrder[$folderName] } else { 1000 }

        $targets.Add([pscustomobject]@{
            FolderName       = $folderName
            SchemaName       = Get-TargetSchemaName -FolderName $folderName
            NormalizedPath   = $normalizedPath
            LocalPath        = $localPath
            SortWeight       = $sortWeight
            HasRunnableFiles = $hasRunnableFiles
            FileCount        = $migrationFiles.Count
        })
    }

    if ($targets.Count -eq 0) {
        throw "No migrations folders were found under '$localSchemasRoot'."
    }

    return $targets | Sort-Object SortWeight, FolderName, NormalizedPath
}

function Get-AppliedMigrationCount {
    param(
        [string]$Container,
        [string]$SqlCmdPath,
        [string]$Password,
        [string]$Database,
        [string]$SchemaName
    )

    $countQuery = "SET NOCOUNT ON; SELECT COUNT(*) FROM [$SchemaName].schema_migrations;"
    $migrationCount = & docker exec $Container $SqlCmdPath -S localhost -U sa -P $Password -C -d $Database -h -1 -W -Q $countQuery 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Migrations ran for schema '$SchemaName', but the final verification query failed."
    }

    return ($migrationCount | Select-Object -Last 1).ToString().Trim()
}

$migrationTargets = @(Get-MigrationTargets -RequestedPaths $MigrationsPath)

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
$migrationTargetsSummary = $migrationTargets | ForEach-Object {
    $targetLabel = "$($_.NormalizedPath) -> [$($_.SchemaName)]"
    if ($_.HasRunnableFiles) {
        "$targetLabel ($($_.FileCount) file(s))"
    }
    else {
        "$targetLabel (placeholder only; no non-empty .sql files yet)"
    }
}

Write-Step "Resolved migration targets"
foreach ($targetDescription in $migrationTargetsSummary) {
    Write-Host " - $targetDescription" -ForegroundColor DarkGray
}

Write-Step "Copying migration runner and schema folders into the container"
Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "exec",
    $ContainerName,
    "/bin/bash",
    "-lc",
    "mkdir -p '$containerWorkDir'"
) -FailureMessage "Failed to prepare a temporary workspace inside '$ContainerName'."

Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "cp",
    $localMigrationRunner,
    "${ContainerName}:${containerWorkDir}/apply_sqlserver_migrations.sh"
) -FailureMessage "Failed to copy the migration runner into '$ContainerName'."

Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "cp",
    $localSchemasRoot,
    "${ContainerName}:${containerWorkDir}/"
) -FailureMessage "Failed to copy schema folders into '$ContainerName'."

Invoke-NativeCommand -FilePath "docker" -ArgumentList @(
    "exec",
    $ContainerName,
    "/bin/bash",
    "-lc",
    "cd '$containerWorkDir' && sed -i 's/\r$//' ./apply_sqlserver_migrations.sh && chmod +x ./apply_sqlserver_migrations.sh"
) -FailureMessage "Failed to normalize the migration runner inside '$ContainerName'."

$migrationResults = New-Object System.Collections.Generic.List[object]

foreach ($migrationTarget in $migrationTargets) {
    $containerCommand = "cd '$containerWorkDir' && bash ./apply_sqlserver_migrations.sh '$($migrationTarget.NormalizedPath)' '$($migrationTarget.SchemaName)'"

    Write-Step "Applying migrations from '$($migrationTarget.NormalizedPath)' to schema '$($migrationTarget.SchemaName)'"
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
    ) -FailureMessage "Failed to apply migrations for schema '$($migrationTarget.SchemaName)'."

    if ($migrationTarget.HasRunnableFiles) {
        $appliedMigrations = Get-AppliedMigrationCount -Container $ContainerName -SqlCmdPath $sqlCmdPath -Password $SaPassword -Database $DatabaseName -SchemaName $migrationTarget.SchemaName
        $migrationResults.Add([pscustomobject]@{
            Path               = $migrationTarget.NormalizedPath
            SchemaName         = $migrationTarget.SchemaName
            AppliedMigrations  = $appliedMigrations
            Note               = $null
        })
    }
    else {
        $migrationResults.Add([pscustomobject]@{
            Path               = $migrationTarget.NormalizedPath
            SchemaName         = $migrationTarget.SchemaName
            AppliedMigrations  = $null
            Note               = "No non-empty .sql files were found, so nothing was recorded."
        })
    }
}

$connectionString = "Server=localhost,1433;Database=$DatabaseName;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;"

Write-Host ""
Write-Host "Local database setup is complete." -ForegroundColor Green
Write-Host "Migration summary:" -ForegroundColor Yellow
foreach ($migrationResult in $migrationResults) {
    if ($migrationResult.Note) {
        Write-Host " - $($migrationResult.Path) -> [$($migrationResult.SchemaName)]: $($migrationResult.Note)" -ForegroundColor DarkGray
    }
    else {
        Write-Host " - $($migrationResult.Path) -> [$($migrationResult.SchemaName)]: $($migrationResult.AppliedMigrations) file(s) recorded in [$($migrationResult.SchemaName)].schema_migrations" -ForegroundColor DarkGray
    }
}
Write-Host ""
Write-Host "AuthService connection string:" -ForegroundColor Yellow
Write-Host $connectionString
Write-Host ""
Write-Host "Next step: run 'dotnet run --project src/AuthService' and test http://localhost:5001/health" -ForegroundColor DarkGray
