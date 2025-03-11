# eShop OpenTelemetry Integration

This implementation adds OpenTelemetry tracing to the eShop application, focusing on the "Place an Order" flow with proper data masking for sensitive information.

## Features Implemented

1. **OpenTelemetry Tracing**
   - End-to-end tracing of the order placement flow
   - Custom activity source with detailed span information
   - Proper trace context propagation

2. **PII Data Protection**
   - Custom OpenTelemetry processor for PII redaction in traces
   - Sensitive data log formatter for filtering PII from logs
   - Masking of credit card numbers, personal information, etc.

3. **Observability Stack**
   - Jaeger for distributed tracing visualization
   - Prometheus for metrics collection
   - Grafana dashboards for comprehensive visualization
   - Custom metrics for business-level monitoring

## Architecture

```
                                    +------------------+
                                    | Docker Container |
                                    +------------------+
                                             |
                                             v
+---------+         +---------------+    +--------+    
| Browser |-------->| Ordering API  |--->| Jaeger |
+---------+   HTTP  | (OTel Tracing)|    +--------+    
                    +---------------+         |
                          |                   |
                          v                   v
                    +-------------+     +-----------+
                    | Metrics     |---->|Prometheus |
                    | Endpoint    |     +-----------+
                    +-------------+          |
                                             v
                                        +----------+
                                        | Grafana  |
                                        | Dashboard|
                                        +----------+
```

Note: As per assignment requirements, only the Ordering API has been instrumented with OpenTelemetry, focusing on the "Place an Order" flow. Other APIs in the eShop application are not connected to the observability stack.

## Implementation Details

### 1. Tracing Configuration

- **ServiceDefaults Extension**: Base OpenTelemetry setup in `eShop.ServiceDefaults`
- **OrderingTelemetry**: Custom ActivitySource for detailed trace spans
- **Telemetry Extensions**: Ordering-specific telemetry configuration
- **PiiRedactionProcessor**: Processor that sanitizes PII from traces

### 2. Metrics Implementation

- **OrderingMetrics**: Custom metrics for business-relevant measurements
- **RequestMetricsMiddleware**: Captures HTTP-level metrics
- **Prometheus Integration**: Exports metrics to Prometheus for visualization

### 3. Log and Sensitive Data Protection

- **SensitiveDataLogFormatter**: Sanitizes log messages to avoid exposing PII
- **PiiRedactionProcessor**: Ensures trace spans don't contain sensitive data
- **Application-level Masking**: Credit card and personal data masked at source

## Setup Instructions

### 1. Start the Observability Stack

```bash
docker-compose -f docker-compose.observability.yml up -d
```

This will start:
- Jaeger (accessible at http://localhost:16686)
- Prometheus (accessible at http://localhost:9090)
- Grafana (accessible at http://localhost:3000, login: admin/admin)

### 2. Start the eShop Application

Build and run the eShop application.

```bash
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### 3. Generate Test Data

Run the load test script to create orders and generate telemetry:

```bash
./order-flow-test.ps1
```

This will create multiple orders and trigger the full order flow with tracing.

In order to run the script, we had to disable authentication in the Ordering API.

### 4. View Traces and Metrics

#### Jaeger UI (http://localhost:16686)
1. Select "OrderingAPI" from the Service dropdown
2. Click "Find Traces" to view traces
3. Click on individual traces to see the full request flow

#### Prometheus (http://localhost:9090)
1. Go to the Graph tab
2. Search for metrics like:
   - `orders_created_total`
   - `payments_processed_total`
   - `order_processing_seconds_bucket`

#### Grafana (http://localhost:3000)
1. Log in with admin/admin
2. Go to Dashboards
3. Open the "eShop Order Flow Dashboard"
4. View metrics and traces in the preconfigured panels

## Verification of Security Measures

To verify sensitive data is properly protected:

1. Create orders with the load test script
2. Check Jaeger spans for the following fields:
   - Credit card information (should show as "[REDACTED]" or masked)
   - Personal information (should be partially masked)
3. Look at console logs to verify PII is not exposed

## Troubleshooting

If you don't see data in the observability tools:

1. Run the `test-metrics.ps1` script to check if metrics are being exported
2. Verify Docker containers are running with `docker ps`
3. Check the Ordering API logs for any OpenTelemetry-related errors
4. Make sure the Ordering API can reach Jaeger at http://localhost:4317

## Key Files

- `src/Ordering.API/Infrastructure/Telemetry/OrderingTelemetry.cs`: Activity source definition
- `src/Ordering.API/Infrastructure/Telemetry/PiiRedactionProcessor.cs`: PII sanitization
- `src/Ordering.API/Infrastructure/Telemetry/OrderingMetrics.cs`: Custom metrics
- `src/Ordering.API/Infrastructure/Telemetry/SensitiveDataLogFormatter.cs`: Log sanitization
- `src/Ordering.API/Extensions/TelemetryExtensions.cs`: Telemetry configuration
- `deployment/grafana/dashboards/ordering-dashboard.json`: Grafana dashboard
- `docker-compose.observability.yml`: Docker setup for observability stack