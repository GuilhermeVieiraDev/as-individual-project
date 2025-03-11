#!/bin/bash

# Test script for generating order flow activity
# This script creates test orders to generate telemetry for the OpenTelemetry assignment

# Default parameter values
COUNT=10
DELAY_MS=500
BASE_URL="http://localhost:5224"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    -c|--count)
      COUNT="$2"
      shift 2
      ;;
    -d|--delay)
      DELAY_MS="$2"
      shift 2
      ;;
    -u|--url)
      BASE_URL="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [-c|--count COUNT] [-d|--delay DELAY_MS] [-u|--url BASE_URL]"
      exit 1
      ;;
  esac
done

# Function to generate a random UUID
generate_uuid() {
  if command -v uuidgen &> /dev/null; then
    uuidgen | tr -d '\n'
  else
    # Fallback if uuidgen is not available
    cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "random-$(date +%s%N)"
  fi
}

# Function to create a test order
create_test_order() {
  local user_id="$1"
  local request_id="$2"
  
  # Format expiration date in ISO 8601 format
  local expiration_date="2028-01-01T00:00:00Z"
  
  # Create order JSON payload
  local order_json=$(cat << EOF
{
  "userId": "$user_id",
  "userName": "Test User $user_id",
  "city": "Seattle",
  "street": "123 Test Street",
  "state": "WA",
  "country": "USA",
  "zipCode": "98001",
  "cardNumber": "4111111111111111",
  "cardHolderName": "Test User",
  "cardExpiration": "$expiration_date",
  "cardSecurityNumber": "123",
  "cardTypeId": 1,
  "buyer": "Test Buyer",
  "items": [
    {
      "productId": 1,
      "productName": "Test Product 1",
      "unitPrice": 19.99,
      "quantity": 2
    },
    {
      "productId": 2,
      "productName": "Test Product 2",
      "unitPrice": 29.99,
      "quantity": 1
    }
  ]
}
EOF
)

  # Send the request
  local response=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -H "x-requestid: $request_id" \
    -d "$order_json" \
    "$BASE_URL/api/orders?api-version=1.0")
  
  # Extract status code from the response
  local status_code=$(echo "$response" | tail -n1)
  local response_body=$(echo "$response" | sed '$d')
  
  # Check if the request was successful
  if [[ $status_code -ge 200 && $status_code -lt 300 ]]; then
    echo "SUCCESS $status_code"
  else
    echo "FAILED $status_code $response_body"
  fi
}

# Main execution
echo "Starting order flow test - Creating $COUNT orders with ${DELAY_MS}ms delay"
echo "Targeting API at: $BASE_URL"
echo "------------------------"

successful=0
failed=0

for i in $(seq 1 $COUNT); do
  user_id="test-user-$i"
  request_id=$(generate_uuid)
  
  echo "Creating order $i/$COUNT for user $user_id (Request ID: $request_id)..."
  
  result=$(create_test_order "$user_id" "$request_id")
  status=$(echo "$result" | cut -d' ' -f1)
  
  if [[ "$status" == "SUCCESS" ]]; then
    status_code=$(echo "$result" | cut -d' ' -f2)
    echo -e "  \e[32mSUCCESS - Status code: $status_code\e[0m"
    ((successful++))
  else
    status_code=$(echo "$result" | cut -d' ' -f2)
    error=$(echo "$result" | cut -d' ' -f3-)
    echo -e "  \e[31mFAILED - Status code: $status_code\e[0m"
    echo -e "  \e[31mError: $error\e[0m"
    ((failed++))
  fi
  
  # Add some random delay between requests
  # Bash doesn't have floating point math, so we'll use bc
  min_delay=$(echo "$DELAY_MS * 0.8" | bc -l | cut -d'.' -f1)
  max_delay=$(echo "$DELAY_MS * 1.2" | bc -l | cut -d'.' -f1)
  actual_delay=$(( $min_delay + RANDOM % ($max_delay - $min_delay + 1) ))
  sleep $(echo "scale=3; $actual_delay/1000" | bc -l)
done

echo "------------------------"
echo "Test completed: $successful successful, $failed failed"
echo "Check your telemetry dashboard and Jaeger UI for the results"