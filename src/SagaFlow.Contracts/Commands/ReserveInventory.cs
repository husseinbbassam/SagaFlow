namespace SagaFlow.Contracts.Commands;

using SagaFlow.Contracts.Events;

public record ReserveInventory(
    Guid OrderId,
    List<OrderItem> Items,
    DateTime RequestedAt);
