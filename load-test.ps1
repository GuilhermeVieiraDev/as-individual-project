# Simple load test script for eShop Order API
# This script sends multiple order creation requests

# Base URL of the API
$API_URL = "http://localhost:5224"

# Number of requests to send
$NUM_REQUESTS = 10

# Function to send order request
function Send-OrderRequest {
    $request_id = [Guid]::NewGuid().ToString()
    
    Write-Host "Sending order request with ID: $request_id"
    
    $randomUserId = "test-user-" + (Get-Random -Minimum 1000 -Maximum 9999)
    
    # Create a UTC DateTime for cardExpiration
    $expirationDate = [DateTime]::SpecifyKind([DateTime]::Parse("2028-01-01T00:00:00"), [DateTimeKind]::Utc)
    $expirationDateString = $expirationDate.ToString("o") # ISO 8601 format with UTC marker
    
    $body = @{
        userId = $randomUserId
        userName = "Test User"
        city = "Seattle"
        street = "123 Main St"
        state = "WA"
        country = "USA"
        zipCode = "98101"
        cardNumber = "4111111111111111"
        cardHolderName = "Test User"
        cardExpiration = $expirationDateString
        cardSecurityNumber = "123"
        cardTypeId = 1
        buyer = "Test Buyer"
        items = @(
            @{
                productId = 1
                productName = "Test Product"
                unitPrice = 10.0
                quantity = 2
            }
        )
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "$API_URL/api/orders?api-version=1.0" -Method Post -ContentType "application/json" -Body $body -Headers @{
            "x-requestid" = $request_id
        }
        Write-Host "Request successful" -ForegroundColor Green
    }
    catch {
        Write-Host "Request failed: $_" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 1
}

Write-Host "Starting load test - sending $NUM_REQUESTS requests..."

# Send multiple requests
for ($i = 1; $i -le $NUM_REQUESTS; $i++) {
    Write-Host "Request $i of $NUM_REQUESTS"
    Send-OrderRequest
}

Write-Host "Load test completed."
