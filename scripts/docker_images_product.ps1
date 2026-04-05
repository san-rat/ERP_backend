# Docker Build and Push All Services Script
# For Windows PowerShell
# This script builds and pushes all 8 microservices to ACR

# Configuration
$ACR_LOGIN_SERVER = "erpacrprod.azurecr.io"
$ACR_USERNAME = "erpacrprod"

# Define all services with their Dockerfile paths
$services = @(
    @{name="apigateway"; dockerfile="src/ApiGateway/dockerfile"; dir="src/ApiGateway"},
    @{name="authservice"; dockerfile="src/AuthService/Dockerfile"; dir="src/AuthService"},
    @{name="analyticsservice"; dockerfile="src/AnalyticsService/Dockerfile"; dir="src/AnalyticsService"},
    @{name="customerservice"; dockerfile="src/CustomerService/Dockerfile"; dir="src/CustomerService"},
    @{name="forecastservice"; dockerfile="src/ForecastService/Dockerfile"; dir="src/ForecastService"},
    @{name="orderservice"; dockerfile="src/OrderService/Dockerfile"; dir="src/OrderService"},
    @{name="predictionservice"; dockerfile="src/PredictionService/Dockerfile"; dir="src/PredictionService"},
    @{name="productservice"; dockerfile="src/ProductService/Dockerfile"; dir="src/ProductService"}
)

# Color output functions
function Write-Header {
    param([string]$Text)
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ">>> $Text" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "CHECKMARK $Text" -ForegroundColor Green
}

function Write-ErrorCustom {
    param([string]$Text)
    Write-Host "X $Text" -ForegroundColor Red
}

# Main script
Write-Header "Starting Docker Build and Push for All Services"
Write-Host "ACR: $ACR_LOGIN_SERVER" -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$failureCount = 0
$startTime = Get-Date

foreach ($service in $services) {
    $serviceName = $service.name
    $dockerfile = $service.dockerfile
    $dir = $service.dir
    $image = "$ACR_LOGIN_SERVER/$($serviceName):bootstrap"
    
    Write-Host ""
    Write-Header "Service: $serviceName"
    
    # BUILD
    Write-Host "Building image..." -ForegroundColor Yellow
    docker build -t $image -f $dockerfile $dir
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorCustom "Build failed for $serviceName"
        $failureCount++
        continue
    }
    
    Write-Success "Build completed for $serviceName"
    
    # PUSH
    Write-Host "Pushing to ACR..." -ForegroundColor Yellow
    docker push $image
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorCustom "Push failed for $serviceName"
        $failureCount++
        continue
    }
    
    Write-Success "$serviceName complete (BUILD + PUSH)"
    $successCount++
}

# Summary
Write-Host ""
Write-Header "FINAL SUMMARY"
Write-Host "Successful: $successCount/8" -ForegroundColor Green
Write-Host "Failed: $failureCount/8" -ForegroundColor $(if ($failureCount -gt 0) {"Red"} else {"Green"})

$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "Total Time: $($duration.Hours)h $($duration.Minutes)m $($duration.Seconds)s" -ForegroundColor Yellow

if ($failureCount -eq 0) {
    Write-Host ""
    Write-Success "ALL IMAGES BUILT AND PUSHED SUCCESSFULLY!"
    Write-Host ""
    Write-Host "Next Step:" -ForegroundColor Cyan
    Write-Host "1. Verify images with: az acr repository list --name erpacrprod --output table" -ForegroundColor Cyan
    Write-Host "2. Go to Azure Portal" -ForegroundColor Cyan
    Write-Host "3. Create Container Apps (images will show up now!)" -ForegroundColor Cyan
}
else {
    Write-ErrorCustom "Some builds or pushes failed. Check the errors above."
}