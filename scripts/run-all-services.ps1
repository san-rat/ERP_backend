# ============================================================
# run-all-services.ps1
# Starts all ERP microservices + ApiGateway in separate windows
# Run from the repo root: .\scripts\run-all-services.ps1
# ============================================================

$root = Split-Path -Parent $PSScriptRoot

$services = @(
    @{ Name = "AuthService";       Port = 5001; Path = "src\AuthService"       },
    @{ Name = "CustomerService";   Port = 5002; Path = "src\CustomerService"   },
    @{ Name = "OrderService";      Port = 5003; Path = "src\OrderService"      },
    @{ Name = "ProductService";    Port = 5004; Path = "src\ProductService"    },
    @{ Name = "ForecastService";   Port = 5005; Path = "src\ForecastService"   },
    @{ Name = "PredictionService"; Port = 5006; Path = "src\PredictionService" },
    @{ Name = "AnalyticsService";  Port = 5007; Path = "src\AnalyticsService"  },
    @{ Name = "ApiGateway";        Port = 5000; Path = "src\ApiGateway"        }
)

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  InsightERP - Starting All Services"        -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

foreach ($svc in $services) {
    $fullPath = Join-Path $root $svc.Path
    $title    = "$($svc.Name) [:$($svc.Port)]"

    Start-Process powershell -ArgumentList `
        "-NoExit", `
        "-Command", `
        "cd '$fullPath'; `$host.UI.RawUI.WindowTitle = '$title'; Write-Host 'Starting $($svc.Name) on port $($svc.Port)...' -ForegroundColor Green; dotnet run"

    Write-Host "  Started  $($svc.Name.PadRight(20)) -> http://localhost:$($svc.Port)" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  All services launched!"                    -ForegroundColor Cyan
Write-Host ""
Write-Host "  Swagger UIs:"                              -ForegroundColor Yellow
foreach ($svc in $services) {
    Write-Host "    $($svc.Name.PadRight(20)) http://localhost:$($svc.Port)/swagger" -ForegroundColor White
}
Write-Host ""
Write-Host "  Gateway health checks (test routing):"    -ForegroundColor Yellow
Write-Host "    http://localhost:5000/health"            -ForegroundColor White
Write-Host "    http://localhost:5000/auth/health"       -ForegroundColor White
Write-Host "    http://localhost:5000/customer/health"   -ForegroundColor White
Write-Host "    http://localhost:5000/order/health"      -ForegroundColor White
Write-Host "    http://localhost:5000/product/health"    -ForegroundColor White
Write-Host "    http://localhost:5000/forecast/health"   -ForegroundColor White
Write-Host "    http://localhost:5000/prediction/health" -ForegroundColor White
Write-Host "    http://localhost:5000/analytics/health"  -ForegroundColor White
Write-Host ""
Write-Host "  Close individual windows to stop each service."        -ForegroundColor DarkGray
Write-Host "  Or run .\scripts\stop-all-services.ps1 to kill all." -ForegroundColor DarkGray
Write-Host "=========================================="              -ForegroundColor Cyan
Write-Host ""
