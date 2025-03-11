using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using eShop.Ordering.API.Infrastructure.Telemetry;
using eShop.Ordering.API.Application.Queries;
using CardType = eShop.Ordering.API.Application.Queries.CardType;
using Order = eShop.Ordering.API.Application.Queries.Order;
using OrderSummary = eShop.Ordering.API.Application.Queries.OrderSummary;

public static class OrdersApi
{
    // PII masking helper methods
    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
            return cardNumber;
            
        // Keep only last 4 digits visible
        if (cardNumber.Length > 4)
            return new string('X', cardNumber.Length - 4) + cardNumber[^4..];
            
        return new string('X', cardNumber.Length); // Mask entire short number
    }

    private static string MaskCardSecurityNumber(string securityNumber)
    {
        if (string.IsNullOrEmpty(securityNumber))
            return securityNumber;
            
        // Replace with same number of 'X' characters to maintain length
        return new string('X', securityNumber.Length);
    }
    
    private static string PartiallyMaskString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        if (input.Length <= 2)
            return input; // Too short to meaningfully mask
            
        if (input.Length <= 4)
            return $"{input[0]}{'*' * (input.Length - 2)}{input[^1]}"; // First and last char visible
            
        return $"{input[..2]}{'*' * (input.Length - 4)}{input[^2..]}"; // First two and last two visible
    }

    public static RouteGroupBuilder MapOrdersApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/orders").HasApiVersion(1.0);

        api.MapPut("/cancel", CancelOrderAsync);
        api.MapPut("/ship", ShipOrderAsync);
        api.MapGet("{orderId:int}", GetOrderAsync);
        api.MapGet("/", GetOrdersByUserAsync);
        api.MapGet("/cardtypes", GetCardTypesAsync);
        api.MapPost("/draft", CreateOrderDraftAsync);
        api.MapPost("/", CreateOrderAsync);

        return api;
    }

    public static async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> CancelOrderAsync(
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancelOrderCommand command,
        [AsParameters] OrderServices services)
    {
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.CancelOrderActivityName);
        activity?.AddTag(OrderingTelemetry.OrderIdTag, command.OrderNumber);
        
        if (requestId == Guid.Empty)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing RequestId");
            OrderingMetrics.ValidationErrors.Add(1);
            OrderingMetrics.RequestErrors.Add(1);
            return TypedResults.BadRequest("Empty GUID is not valid for request ID");
        }

        var requestCancelOrder = new IdentifiedCommand<CancelOrderCommand, bool>(command, requestId);

        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            requestCancelOrder.GetGenericTypeName(),
            nameof(requestCancelOrder.Command.OrderNumber),
            requestCancelOrder.Command.OrderNumber,
            requestCancelOrder);

        var commandResult = await services.Mediator.Send(requestCancelOrder);

        if (!commandResult)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Cancel order failed");
            OrderingMetrics.ProcessingErrors.Add(1);
            return TypedResults.Problem(detail: "Cancel order failed to process.", statusCode: 500);
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return TypedResults.Ok();
    }

    public static async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> ShipOrderAsync(
        [FromHeader(Name = "x-requestid")] Guid requestId,
        ShipOrderCommand command,
        [AsParameters] OrderServices services)
    {
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.ShipOrderActivityName);
        activity?.AddTag(OrderingTelemetry.OrderIdTag, command.OrderNumber);
        
        if (requestId == Guid.Empty)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing RequestId");
            OrderingMetrics.ValidationErrors.Add(1);
            OrderingMetrics.RequestErrors.Add(1);
            return TypedResults.BadRequest("Empty GUID is not valid for request ID");
        }

        var requestShipOrder = new IdentifiedCommand<ShipOrderCommand, bool>(command, requestId);

        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            requestShipOrder.GetGenericTypeName(),
            nameof(requestShipOrder.Command.OrderNumber),
            requestShipOrder.Command.OrderNumber,
            requestShipOrder);

        var commandResult = await services.Mediator.Send(requestShipOrder);

        if (!commandResult)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Ship order failed");
            OrderingMetrics.ProcessingErrors.Add(1);
            return TypedResults.Problem(detail: "Ship order failed to process.", statusCode: 500);
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<Order>, NotFound>> GetOrderAsync(int orderId, [AsParameters] OrderServices services)
    {
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.GetOrderActivityName);
        activity?.AddTag(OrderingTelemetry.OrderIdTag, orderId);
        
        try
        {
            var order = await services.Queries.GetOrderAsync(orderId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return TypedResults.Ok(order);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            OrderingMetrics.ProcessingErrors.Add(1);
            return TypedResults.NotFound();
        }
    }

    public static async Task<Ok<IEnumerable<OrderSummary>>> GetOrdersByUserAsync([AsParameters] OrderServices services)
    {
        var userId = services.IdentityService.GetUserIdentity();
        
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.GetOrdersByUserActivityName);
        activity?.AddTag(OrderingTelemetry.UserIdTag, PartiallyMaskString(userId));
        
        var orders = await services.Queries.GetOrdersFromUserAsync(userId);
        activity?.SetStatus(ActivityStatusCode.Ok);
        
        return TypedResults.Ok(orders);
    }

    public static async Task<Ok<IEnumerable<CardType>>> GetCardTypesAsync(IOrderQueries orderQueries)
    {
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.GetCardTypesActivityName);
        
        var cardTypes = await orderQueries.GetCardTypesAsync();
        activity?.SetStatus(ActivityStatusCode.Ok);
        
        return TypedResults.Ok(cardTypes);
    }

    public static async Task<OrderDraftDTO> CreateOrderDraftAsync(CreateOrderDraftCommand command, [AsParameters] OrderServices services)
    {
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.CreateOrderDraftActivityName);
        activity?.AddTag(OrderingTelemetry.UserIdTag, PartiallyMaskString(command.BuyerId));
        
        // Don't log the entire command object as it may contain PII
        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId}",
            command.GetGenericTypeName(),
            nameof(command.BuyerId),
            PartiallyMaskString(command.BuyerId));

        var result = await services.Mediator.Send(command);
        activity?.SetStatus(ActivityStatusCode.Ok);
        
        return result;
    }

    public static async Task<Results<Ok, BadRequest<string>>> CreateOrderAsync(
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CreateOrderRequest request,
        [AsParameters] OrderServices services)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = OrderingTelemetry.Source.StartActivity(OrderingTelemetry.CreateOrderActivityName);
        
        // Add activity tags with PII masking
        activity?.AddTag(OrderingTelemetry.UserIdTag, PartiallyMaskString(request.UserId));
        activity?.AddTag(OrderingTelemetry.OrderItemsCountTag, request.Items?.Count ?? 0);
        activity?.AddTag(OrderingTelemetry.ShippingCountryTag, request.Country);
        
        // Add additional tags with proper PII handling
        activity?.AddTag(OrderingTelemetry.UserNameTag, PartiallyMaskString(request.UserName));
        activity?.AddTag(OrderingTelemetry.PaymentMethodTag, request.CardTypeId.ToString());
        
        // Only log essential, non-sensitive information
        services.Logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId}",
            request.GetGenericTypeName(),
            nameof(request.UserId),
            request.UserId);

        if (requestId == Guid.Empty)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Missing RequestId");
            services.Logger.LogWarning("Invalid order request - RequestId is missing");
            OrderingMetrics.ValidationErrors.Add(1);
            OrderingMetrics.RequestErrors.Add(1);
            return TypedResults.BadRequest("RequestId is missing.");
        }

        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.CardNumber) || request.Items == null || !request.Items.Any())
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid order data");
            services.Logger.LogWarning("Invalid order data - RequestId: {RequestId}", requestId);
            OrderingMetrics.ValidationErrors.Add(1);
            return TypedResults.BadRequest("Invalid order data.");
        }

        using (services.Logger.BeginScope(new List<KeyValuePair<string, object>> { new("IdentifiedCommandId", requestId) }))
        {
            // Enhance masking for card data
            var maskedCCNumber = MaskCardNumber(request.CardNumber);
            var maskedCCSecurityNumber = MaskCardSecurityNumber(request.CardSecurityNumber);
            
            // Create the command without logging sensitive payment details
            var createOrderCommand = new CreateOrderCommand(
                request.Items, 
                request.UserId, 
                request.UserName, 
                request.City, 
                request.Street,
                request.State, 
                request.Country, 
                request.ZipCode,
                maskedCCNumber, 
                request.CardHolderName, // Will be masked by PII processor
                request.CardExpiration,
                maskedCCSecurityNumber, // Masked for logging
                request.CardTypeId);

            var requestCreateOrder = new IdentifiedCommand<CreateOrderCommand, bool>(createOrderCommand, requestId);

            services.Logger.LogInformation(
                "Sending command: {CommandName} - {IdProperty}: {CommandId}",
                requestCreateOrder.GetGenericTypeName(),
                nameof(requestCreateOrder.Id),
                requestCreateOrder.Id);

            var result = await services.Mediator.Send(requestCreateOrder);

            stopwatch.Stop();
            
            if (result)
            {
                // Record successful metrics
                OrderingMetrics.OrdersCreated.Add(1);
                OrderingMetrics.PaymentProcessed.Add(1);
                OrderingMetrics.OrderProcessingTime.Record(stopwatch.Elapsed.TotalSeconds);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
                services.Logger.LogInformation("CreateOrderCommand succeeded - RequestId: {RequestId}", requestId);
            }
            else
            {
                OrderingMetrics.ProcessingErrors.Add(1);
                activity?.SetStatus(ActivityStatusCode.Error, "Create order failed");
                services.Logger.LogWarning("CreateOrderCommand failed - RequestId: {RequestId}", requestId);
            }

            return TypedResults.Ok();
        }
    }
}

public record CreateOrderRequest(
    string UserId,
    string UserName,
    string City,
    string Street,
    string State,
    string Country,
    string ZipCode,
    string CardNumber,
    string CardHolderName,
    DateTime CardExpiration,
    string CardSecurityNumber,
    int CardTypeId,
    string Buyer,
    List<BasketItem> Items);
