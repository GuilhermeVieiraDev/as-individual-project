#!/bin/bash

# Simple error test script for eShop Order API
# This script generates various error types for testing metrics

# Base URL of the API
API_URL="http://localhost:5224"

# Function to generate a random UUID
generate_uuid() {
  if command -v uuidgen &> /dev/null; then
    uuidgen | tr -d '\n'
  else
    # Fallback if uuidgen is not available
    cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "random-$(date +%s%N)"
  fi
}

# Function to generate a random number between min and max
random_number() {
  local min=$1
  local max=$2
  echo $(( $min + RANDOM % ($max - $min + 1) ))
}

# Function to send a valid order request
send_valid_order_request() {
  local request_id=$(generate_uuid)
  
  echo "Sending valid order request with ID: $request_id"
  
  local random_user_id="test-user-$(random_number 1000 9999)"
  
  # Format expiration date in ISO 8601 format
  local expiration_date="2028-01-01T00:00:00Z"
  
  # Create order JSON payload
  local body=$(cat << EOF
{
  "userId": "$random_user_id",
  "userName": "Test User",
  "city": "Seattle",
  "street": "123 Main St",
  "state": "WA",
  "country": "USA",
  "zipCode": "98101",
  "cardNumber": "4111111111111111",
  "cardHolderName": "Test User",
  "cardExpiration": "$expiration_date",
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
}
EOF
)
  
  # Send request
  local response=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -H "x-requestid: $request_id" \
    -d "$body" \
    "$API_URL/api/orders?api-version=1.0")
  
  # Extract status code from the response
  local status_code=$(echo "$response" | tail -n1)
  
  # Check if the request was successful
  if [[ $status_code -ge 200 && $status_code -lt 300 ]]; then
    echo -e "\e[32mRequest successful\e[0m"
  else
    echo -e "\e[31mRequest failed with status code: $status_code\e[0m"
  fi
}

# Function to send order without request ID (should cause error)
send_order_without_request_id() {
  echo -e "\e[33mSending order without request ID (should cause error)\e[0m"
  
  local random_user_id="test-user-$(random_number 1000 9999)"
  
  # Format expiration date in ISO 8601 format
  local expiration_date="2028-01-01T00:00:00Z"
  
  # Create order JSON payload
  local body=$(cat << EOF
{
  "userId": "$random_user_id",
  "userName": "Test User",
  "city": "Seattle",
  "street": "123 Main St",
  "state": "WA",
  "country": "USA",
  "zipCode": "98101",
  "cardNumber": "4111111111111111",
  "cardHolderName": "Test User",
  "cardExpiration": "$expiration_date",
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
}
EOF
)
  
  # Send request without request ID header
  local response=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "$body" \
    "$API_URL/api/orders?api-version=1.0")
  
  # Extract status code from the response
  local status_code=$(echo "$response" | tail -n1)
  
  # Check if the request failed as expected
  if [[ $status_code -ge 400 ]]; then
    echo -e "\e[32mRequest failed as expected\e[0m"
  else
    echo -e "\e[33mRequest unexpectedly succeeded\e[0m"
  fi
}

# Function to send request to non-existent endpoint (404 error)
send_request_to_non_existent_endpoint() {
  echo -e "\e[33mSending request to non-existent endpoint (should cause 404)\e[0m"
  
  # Send request to non-existent endpoint
  local response=$(curl -s -w "\n%{http_code}" -X GET \
    "$API_URL/api/non-existent-endpoint")
  
  # Extract status code from the response
  local status_code=$(echo "$response" | tail -n1)
  
  # Check if the request failed as expected
  if [[ $status_code -eq 404 ]]; then
    echo -e "\e[32mRequest failed as expected\e[0m"
  else
    echo -e "\e[33mRequest unexpectedly returned status code: $status_code\e[0m"
  fi
}

# Function to request non-existent order
send_request_for_non_existent_order() {
  echo -e "\e[33mRequesting non-existent order (should cause error)\e[0m"
  
  local random_order_id=$(random_number 99999 999999)
  
  # Send request for non-existent order
  local response=$(curl -s -w "\n%{http_code}" -X GET \
    "$API_URL/api/orders/$random_order_id")
  
  # Extract status code from the response
  local status_code=$(echo "$response" | tail -n1)
  
  # Check if the request failed as expected
  if [[ $status_code -ge 400 ]]; then
    echo -e "\e[32mRequest failed as expected\e[0m"
  else
    echo -e "\e[33mRequest unexpectedly succeeded\e[0m"
  fi
}

echo "Starting error test..."

# Send valid requests
send_valid_order_request
send_valid_order_request
sleep 1

# Send error-causing requests
send_order_without_request_id
sleep 1

send_request_to_non_existent_endpoint
sleep 1

send_request_for_non_existent_order
sleep 1

# Send more valid requests
send_valid_order_request
send_valid_order_request
sleep 1

# Send more error-causing requests
send_order_without_request_id
sleep 1

send_request_to_non_existent_endpoint
sleep 1

echo "Error test completed."