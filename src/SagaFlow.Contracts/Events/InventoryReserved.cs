namespace SagaFlow.Contracts.Events;

public record InventoryReserved(
    Guid OrderId,
    string ReservationId,
    DateTime ReservedAt);
