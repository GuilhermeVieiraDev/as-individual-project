using eShop.Ordering.API.Extensions;
using eShop.Ordering.API.Infrastructure.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.AddOrderingTelemetry(); // Add our telemetry configuration
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
