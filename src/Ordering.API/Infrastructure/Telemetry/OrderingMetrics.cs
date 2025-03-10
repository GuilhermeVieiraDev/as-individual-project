using System.Diagnostics.Metrics;
using eShop.Ordering.API.Extensions;

namespace eShop.Ordering.API.Infrastructure.Telemetry;

public static class OrderingMetrics
{
    // Create a meter with the same name as our ActivitySource for consistency
    public static readonly Meter Meter = new Meter("eShop.Ordering");
    
    // Define some counters and gauges
    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>(
        "orders_created_total",
        description: "Total number of orders created");
    
    public static readonly Counter<long> PaymentProcessed = Meter.CreateCounter<long>(
        "payments_processed_total", 
        description: "Total number of payments processed");
        
    public static readonly Histogram<double> OrderProcessingTime = Meter.CreateHistogram<double>(
        "order_processing_seconds",
        unit: "s",
        description: "Time to process an order");
        
    // Add error counters
    public static readonly Counter<long> RequestErrors = Meter.CreateCounter<long>(
        "request_errors_total",
        description: "Total number of request errors");
        
    public static readonly Counter<long> ValidationErrors = Meter.CreateCounter<long>(
        "validation_errors_total",
        description: "Total number of validation errors");
        
    public static readonly Counter<long> ProcessingErrors = Meter.CreateCounter<long>(
        "processing_errors_total",
        description: "Total number of order processing errors");
}
