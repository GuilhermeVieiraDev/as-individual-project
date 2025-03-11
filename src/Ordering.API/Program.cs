using eShop.Ordering.API.Extensions;
using eShop.Ordering.API.Infrastructure.Middlewares;
using eShop.Ordering.API.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (includes OpenTelemetry setup)
builder.AddServiceDefaults();
builder.AddApplicationServices();

// Add our custom telemetry configuration
builder.AddOrderingTelemetry();

// Configure sensitive data filtering for logs AFTER other logging is configured
// to avoid circular dependencies
builder.Logging.AddSensitiveDataFilter();

builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

// Add our request metrics middleware before other middleware
app.UseRequestMetrics();

app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");

orders.MapOrdersApiV1();
      // .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
