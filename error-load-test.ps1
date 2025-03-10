# Simple error test script for eShop Order API
# This script generates various error types for testing metrics

# Base URL of the API
$API_URL = "http://localhost:5224"

# Function to send a valid order request
function Send-ValidOrderRequest {
    $request_id = [Guid]::NewGuid().ToString()
    
    Write-Host "Sending valid order request with ID: $request_id"
    
    $randomUserId = "test-user-" + (Get-Random -Minimum 1000 -Maximum 9999)
    
    $expirationDate = Get-Date -Year 2028 -Month 1 -Day 1
    
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
        cardExpiration = $expirationDate
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
}

# Function to send order without request ID (should cause error)
function Send-OrderWithoutRequestId {
    Write-Host "Sending order without request ID (should cause error)" -ForegroundColor Yellow
    
    $randomUserId = "test-user-" + (Get-Random -Minimum 1000 -Maximum 9999)
    
    $expirationDate = Get-Date -Year 2028 -Month 1 -Day 1
    
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
        cardExpiration = $expirationDate
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
        Invoke-RestMethod -Uri "$API_URL/api/orders?api-version=1.0" -Method Post -ContentType "application/json" -Body $body
        Write-Host "Request unexpectedly succeeded" -ForegroundColor Yellow
    }
    catch {
        Write-Host "Request failed as expected" -ForegroundColor Green
    }
}

# Function to send request to non-existent endpoint (404 error)
function Send-RequestToNonExistentEndpoint {
    Write-Host "Sending request to non-existent endpoint (should cause 404)" -ForegroundColor Yellow
    
    try {
        Invoke-RestMethod -Uri "$API_URL/api/non-existent-endpoint" -Method Get
        Write-Host "Request unexpectedly succeeded" -ForegroundColor Yellow
    }
    catch {
        Write-Host "Request failed as expected" -ForegroundColor Green
    }
}

# Function to request non-existent order
function Send-RequestForNonExistentOrder {
    Write-Host "Requesting non-existent order (should cause error)" -ForegroundColor Yellow
    
    $randomOrderId = Get-Random -Minimum 99999 -Maximum 999999
    
    try {
        Invoke-RestMethod -Uri "$API_URL/api/orders/$randomOrderId" -Method Get
        Write-Host "Request unexpectedly succeeded" -ForegroundColor Yellow
    }
    catch {
        Write-Host "Request failed as expected" -ForegroundColor Green
    }
}

Write-Host "Starting error test..."

# Send valid requests
Send-ValidOrderRequest
Send-ValidOrderRequest
Start-Sleep -Seconds 1

# Send error-causing requests
Send-OrderWithoutRequestId
Start-Sleep -Seconds 1

Send-RequestToNonExistentEndpoint
Start-Sleep -Seconds 1

Send-RequestForNonExistentOrder
Start-Sleep -Seconds 1

# Send more valid requests
Send-ValidOrderRequest
Send-ValidOrderRequest
Start-Sleep -Seconds 1

# Send more error-causing requests
Send-OrderWithoutRequestId
Start-Sleep -Seconds 1

Send-RequestToNonExistentEndpoint
Start-Sleep -Seconds 1

Write-Host "Error test completed."