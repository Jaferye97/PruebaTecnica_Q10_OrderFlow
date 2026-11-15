using System.Text.Json;

namespace OrderFlow.Contracts;

/// <summary>
/// Lo que ambos servicios deben acordar para hablar entre sí: exchanges, routing keys,
/// nombres de tipo (propiedad Type del mensaje) y formato JSON. Las colas son de cada servicio.
/// </summary>
public static class Messaging
{
    public const string Exchange = "orderflow.events";
    public const string DeadLetterExchange = "orderflow.dlx";

    public static class RoutingKeys
    {
        public const string OrderCreated = "order.created";
        public const string StockReserved = "stock.reserved";
        public const string StockRejected = "stock.rejected";

        /// <summary>Binding de la cola de resultados de Orders.</summary>
        public const string AllStockResults = "stock.*";
    }

    /// <summary>Nombre que viaja en la propiedad Type del mensaje.</summary>
    public static string TypeName(IIntegrationEvent integrationEvent) => integrationEvent.GetType().Name;

    public static string RoutingKeyFor(string typeName) => typeName switch
    {
        nameof(OrderCreated) => RoutingKeys.OrderCreated,
        nameof(StockReserved) => RoutingKeys.StockReserved,
        nameof(StockRejected) => RoutingKeys.StockRejected,
        _ => throw new ArgumentException($"Tipo de evento desconocido: '{typeName}'.", nameof(typeName))
    };

    /// <summary>camelCase y sin distinguir mayúsculas al leer.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}
