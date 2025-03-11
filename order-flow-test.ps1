# Test script for generating order flow activity
# This script creates test orders to generate telemetry for the OpenTelemetry assignment

param (
    [int]$Count = 10,
    [int]$DelayMs = 500,
    [string]$BaseUrl = "http://localhost:5224"
)

# Function to generate a random GUID
function New-Guid {
    return [guid]::NewGuid().ToString()
}

# Function to create a test order
function Create-TestOrder {
    param (
        [string]$UserId,
        [string]$RequestId
    )
    
    $orderItems = @(
        @{
            productId = 1
            productName = "Test Product 1"
            unitPrice = 19.99
            quantity = 2
        },
        @{
            productId = 2
            productName = "Test Product 2"
            unitPrice = 29.99
            quantity = 1
        }
    )

    $expirationDate = [DateTime]::SpecifyKind([DateTime]::Parse("2028-01-01T00:00:00"), [DateTimeKind]::Utc)
    $expirationDateString = $expirationDate.ToString("o") # ISO 8601 format with UTC marker
    
    $order = @{
        userId = $UserId
        userName = "Test User $UserId"
        city = "Seattle"
        street = "123 Test Street"
        state = "WA"
        country = "USA"
        zipCode = "98001"
        cardNumber = "4111111111111111"
        cardHolderName = "Test User"
        cardExpiration = $expirationDateString
        cardSecurityNumber = "123"
        cardTypeId = 1
        buyer = "Test Buyer"
        items = $orderItems
    }
    
    $orderJson = $order | ConvertTo-Json -Depth 3
    
    $headers = @{
        "Content-Type" = "application/json"
        "x-requestid" = $RequestId
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/orders?api-version=1.0" -Method Post -Body $orderJson -Headers $headers
        return @{
            StatusCode = $response.StatusCode
            RequestId = $RequestId
            Success = $true
        }
    }
    catch {
        return @{
            StatusCode = $_.Exception.Response.StatusCode.value__
            RequestId = $RequestId
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Main execution
Write-Host "Starting order flow test - Creating $Count orders with ${DelayMs}ms delay"
Write-Host "Targeting API at: $BaseUrl"
Write-Host "------------------------"

$successful = 0
$failed = 0

for ($i = 1; $i -le $Count; $i++) {
    $userId = "test-user-$i"
    $requestId = New-Guid
    
    Write-Host "Creating order $i/$Count for user $userId (Request ID: $requestId)..."
    
    $result = Create-TestOrder -UserId $userId -RequestId $requestId
    
    if ($result.Success) {
        Write-Host "  SUCCESS - Status code: $($result.StatusCode)" -ForegroundColor Green
        $successful++
    }
    else {
        Write-Host "  FAILED - Status code: $($result.StatusCode)" -ForegroundColor Red
        Write-Host "  Error: $($result.Error)" -ForegroundColor Red
        $failed++
    }
    
    # Add some random delay between requests
    $actualDelay = Get-Random -Minimum ($DelayMs * 0.8) -Maximum ($DelayMs * 1.2)
    Start-Sleep -Milliseconds $actualDelay
}

Write-Host "------------------------"
Write-Host "Test completed: $successful successful, $failed failed"
Write-Host "Check your telemetry dashboard and Jaeger UI for the results"
