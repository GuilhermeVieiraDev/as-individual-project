using System.Diagnostics;

namespace eShop.Ordering.API.Infrastructure.Telemetry;

public static class OrderingTelemetry
{
    public static readonly ActivitySource Source = new ActivitySource("eShop.Ordering");
    
    // Activity names as constants
    public const string CreateOrderActivityName = "CreateOrder";
    public const string ProcessPaymentActivityName = "ProcessPayment";
    public const string GetOrderActivityName = "GetOrder";
    public const string GetOrdersByUserActivityName = "GetOrdersByUser";
    public const string GetCardTypesActivityName = "GetCardTypes";
    public const string CreateOrderDraftActivityName = "CreateOrderDraft";
    public const string UpdateOrderDraftActivityName = "UpdateOrderDraft";
    public const string CancelOrderActivityName = "CancelOrder";
    public const string ShipOrderActivityName = "ShipOrder";
    
    // Tag names as constants for consistent usage
    public const string OrderIdTag = "order.id";
    public const string UserIdTag = "user.id";
    public const string OrderItemsCountTag = "order.items.count";
    public const string OrderStatusTag = "order.status";
    public const string ShippingCountryTag = "order.shipping.country";
    
    // Additional tag names for more detailed context
    public const string UserNameTag = "user.name";
    public const string UserCityTag = "user.city";
    public const string UserStateTag = "user.state";
    public const string UserCountryTag = "user.country";
    public const string PaymentMethodTag = "payment.method";
    public const string PaymentProviderTag = "payment.provider";
    public const string OrderTotalTag = "order.total";
    public const string OrderCurrencyTag = "order.currency";
    public const string OrderDateTag = "order.date";
    public const string ErrorTypeTag = "error.type";
    public const string ErrorMessageTag = "error.message";
    public const string ErrorCodeTag = "error.code";
    
    // For database operations
    public const string DbOperationTag = "db.operation";
    public const string DbStatementTag = "db.statement";
    public const string DbTableTag = "db.table";
    
    // For message broker operations
    public const string MessageTypeTag = "messaging.message_type";
    public const string MessageIdTag = "messaging.message_id";
    public const string MessageDestinationTag = "messaging.destination";
}
