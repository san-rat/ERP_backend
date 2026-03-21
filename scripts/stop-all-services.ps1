# ============================================================
# stop-all-services.ps1
# Kills all running dotnet processes (stops all microservices)
# Run from the repo root: .\scripts\stop-all-services.ps1
# ============================================================

Write-Host ""
Write-Host "Stopping all dotnet services..." -ForegroundColor Yellow

$procs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

if ($procs.Count -eq 0) {
    Write-Host "No dotnet processes found running." -ForegroundColor DarkGray
} else {
    $procs | ForEach-Object {
        Write-Host "  Stopping PID $($_.Id)..." -ForegroundColor Red
        Stop-Process -Id $_.Id -Force
    }
    Write-Host ""
    Write-Host "All services stopped." -ForegroundColor Green
}

Write-Host ""
