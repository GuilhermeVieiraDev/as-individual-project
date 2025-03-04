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
   - Configured Grafana for visualization
   - Created custom dashboards for the order flow

## Architecture

```
   Client                 Ordering API                    Jaeger
+---------+    HTTP    +-------------+    OTLP     +---------------+
| Browser | ---------> | ASP.NET Core| ----------> | Trace Storage |
+---------+            +-------------+             +---------------+
                             |                            |
                             |                            |
                       +-------------+              +------------+
                       | Database    |              | Grafana    |
                       +-------------+              +------------+
```

## Implementation Details

### 1. Tracing Components

- **OrderingTelemetry**: Custom ActivitySource for creating spans
- **PiiRedactionProcessor**: Processor that redacts sensitive information from spans
- **TelemetryExtensions**: Extension methods for configuration

### 2. Data Security

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
- Grafana (accessible at http://localhost:3000)

### 2. Start the eShop Application

Follow the standard eShop startup procedure, but ensure the OTLP endpoint environment variables are set:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

### 3. Run the Load Test

```bash
chmod +x load-test.sh
./load-test.sh
```

### 4. View Traces in Grafana

1. Open Grafana at http://localhost:3000
   - Username: admin
   - Password: admin
2. Navigate to the "eShop Order Flow" dashboard

## Verifying Sensitive Data Protection

1. Execute several orders via the load test script
2. Check Jaeger spans for any of these fields:
   - Credit card information
   - Personal information
3. Verify the data is properly masked/redacted

## Future Enhancements

- Add more business metrics
- Expand tracing to other microservices
- Implement database column masking for persistent data
