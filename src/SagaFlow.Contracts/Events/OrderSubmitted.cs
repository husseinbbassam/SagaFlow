namespace SagaFlow.Contracts.Events;

public record OrderSubmitted(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    List<OrderItem> Items,
    DateTime Timestamp);

public record OrderItem(
    string ProductId,
    int Quantity,
    decimal Price);
