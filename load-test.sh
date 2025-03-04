#!/bin/bash

# Simple load test script for eShop Order API
# This script sends multiple order creation requests

# Base URL of the API
API_URL="http://localhost:5102"

# Number of requests to send
NUM_REQUESTS=10

# Function to generate a random GUID
generate_guid() {
  python -c "import uuid; print(str(uuid.uuid4()))"
}

# Function to send order request
send_order_request() {
  local request_id=$(generate_guid)
  
  echo "Sending order request with ID: $request_id"
  
  curl -X POST \
    "$API_URL/api/orders" \
    -H "Content-Type: application/json" \
    -H "x-requestid: $request_id" \
    -d '{
      "userId": "test-user-'$RANDOM'",
      "userName": "Test User",
      "city": "Seattle",
      "street": "123 Main St",
      "state": "WA",
      "country": "USA",
      "zipCode": "98101",
      "cardNumber": "4111111111111111",
      "cardHolderName": "Test User",
      "cardExpiration": "2028-01-01T00:00:00",
      "cardSecurityNumber": "123",
      "cardTypeId": 1,
      "buyer": "Test Buyer",
      "items": [
        {
          "productId": 1,
          "productName": "Test Product",
          "unitPrice": 10.0,
          "quantity": 2
        }
      ]
    }'
  
  echo ""
  sleep 1
}

echo "Starting load test - sending $NUM_REQUESTS requests..."

# Send multiple requests
for (( i=1; i<=$NUM_REQUESTS; i++ ))
do
   echo "Request $i of $NUM_REQUESTS"
   send_order_request
done

echo "Load test completed."
