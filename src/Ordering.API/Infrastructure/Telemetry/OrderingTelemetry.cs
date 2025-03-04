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
}
