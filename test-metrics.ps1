# Test script to verify OpenTelemetry metrics are being properly exported
# Run this script to check if your metrics endpoint is working

# First, try to access the metrics endpoint directly
Write-Host "Testing metrics endpoint..." -ForegroundColor Cyan
Write-Host "Attempting to access /metrics endpoint on various ports..."

$ports = @(5231, 5224, 5106, 80, 443)
$foundMetrics = $false

foreach ($port in $ports) {
    Write-Host "Testing port $port..." -NoNewline
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$port/metrics" -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "SUCCESS!" -ForegroundColor Green
            Write-Host "Found metrics endpoint at http://localhost:$port/metrics"
            Write-Host "Sample metrics:" -ForegroundColor Yellow
            $sampleLines = $response.Content -split "`n" | Select-Object -First 10
            $sampleLines | ForEach-Object { Write-Host "  $_" }
            Write-Host "..."
            $foundMetrics = $true
            break
        }
    }
    catch {
        Write-Host "Failed" -ForegroundColor Red
    }
}

if (-not $foundMetrics) {
    Write-Host "Could not find metrics endpoint on any of the checked ports." -ForegroundColor Red
    Write-Host "Make sure your application is running and has the Prometheus exporter enabled." -ForegroundColor Yellow
}

# Now check if Prometheus is seeing the metrics
Write-Host "`nTesting Prometheus configuration..." -ForegroundColor Cyan
Write-Host "Attempting to access Prometheus API to check targets..."

try {
    $targetsResponse = Invoke-WebRequest -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 5 -ErrorAction Stop
    $targets = ($targetsResponse.Content | ConvertFrom-Json).data.activeTargets
    
    Write-Host "Found ${targets.Count} targets in Prometheus:" -ForegroundColor Yellow
    foreach ($target in $targets) {
        $status = if ($target.health -eq "up") { "UP" } else { "DOWN" }
        $color = if ($target.health -eq "up") { "Green" } else { "Red" }
        Write-Host "  $($target.labels.job) ($($target.labels.instance)): $status" -ForegroundColor $color
        
        if ($target.health -ne "up") {
            Write-Host "    Last error: $($target.lastError)" -ForegroundColor Red
        }
    }
}
catch {
    Write-Host "Failed to connect to Prometheus API: $_" -ForegroundColor Red
}

Write-Host "`nTesting completed." -ForegroundColor Cyan
