# eShop OpenTelemetry Integration

This implementation adds OpenTelemetry tracing to the eShop application, focusing on the "Place an Order" flow with proper data masking for sensitive information.

## Features Implemented

1. **OpenTelemetry Tracing**
   - Added tracing to the entire order flow
   - Instrumented all API endpoints in the Ordering service
   - Configured custom activity source for detailed spans

2. **PII Data Protection**
   - Implemented custom OpenTelemetry processor for PII redaction
   - Masked sensitive data in logs (credit card numbers, personal details)
   - Ensured no sensitive data leaks in traces

3. **Observability Stack**
   - Set up Jaeger for distributed tracing
   - Set up Prometheus for metrics collection
   - Configured Grafana for visualization of both traces and metrics
   - Created custom dashboards for the order flow

## Architecture

```
   Client                 Ordering API                  Observability Stack
+---------+    HTTP    +-------------+    OTLP     +---------------+
| Browser | ---------> | ASP.NET Core| ----------> | Jaeger        |
+---------+            +-------------+             | (Traces)      |
                             |                     +---------------+
                             |                            |
                       +-------------+                    v
                       | Database    |             +---------------+
                       +-------------+             | Prometheus    |
                             ^                     | (Metrics)     |
                             |                     +---------------+
                       +-------------+                    |
                       | Metrics     |                    v
                       | Endpoint    |             +---------------+
                       +-------------+             | Grafana       |
                                                   | (Dashboards)  |
                                                   +---------------+
```

## Implementation Details

### 1. Tracing Components

- **OrderingTelemetry**: Custom ActivitySource for creating spans
- **PiiRedactionProcessor**: Processor that redacts sensitive information from spans
- **TelemetryExtensions**: Extension methods for configuration

### 2. Metrics Components

- **Prometheus Exporter**: Exposes metrics at the `/metrics` endpoint
- **Metric Instrumentation**: HTTP requests, database operations, etc.
- **Custom Metrics**: Business-relevant metrics for the ordering process

### 3. Data Security

The implementation includes several layers of protection:

- Credit card numbers are masked at the application level
- PII redaction in spans via custom processor
- Log filtering to prevent sensitive data from appearing in logs

## Setup Instructions

### 1. Start the Observability Stack

```bash
docker-compose -f docker-compose.observability.yml up -d
```

This starts:
- Jaeger (accessible at http://localhost:16686)
- Prometheus (accessible at http://localhost:9090)
- Grafana (accessible at http://localhost:3000)

### 2. Start the eShop Application

Follow the standard eShop startup procedure, but ensure the OTLP endpoint environment variables are set:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

### 3. Run the Load Test

```bash
./load-test.ps1
```

### 4. View Data in Grafana

1. Open Grafana at http://localhost:3000
   - Username: admin
   - Password: admin
2. Navigate to the "eShop Order Flow" dashboard
3. You'll see panels showing both:
   - Trace data from Jaeger
   - Metrics data from Prometheus

## Verifying Sensitive Data Protection

1. Execute several orders via the load test script
2. Check Jaeger spans for any of these fields:
   - Credit card information
   - Personal information
3. Verify the data is properly masked/redacted

## Benefits of Using Prometheus

1. **Metrics Collection**: Collects and stores time-series data about API requests, errors, and system-level metrics
2. **Long-term Trend Analysis**: Allows tracking performance patterns over time
3. **Alerting Capabilities**: Can be used to set up alerts when metrics exceed thresholds
4. **Complementary to Tracing**: When used alongside Jaeger, provides a complete observability solution
   - Traces show detailed information about individual requests
   - Metrics show aggregated data about system behavior

## Future Enhancements

- Add custom business metrics to measure order flow health
- Set up alerting based on key metrics
- Expand tracing to other microservices
- Implement database column masking for persistent data
