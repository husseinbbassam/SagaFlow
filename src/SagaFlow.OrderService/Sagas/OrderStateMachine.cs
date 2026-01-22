using MassTransit;
using SagaFlow.Contracts.Commands;
using SagaFlow.Contracts.Events;

namespace SagaFlow.OrderService.Sagas;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State? ProcessingPayment { get; private set; }
    public State? PaymentProcessed { get; private set; }
    public State? ReservingInventory { get; private set; }
    public State? Completed { get; private set; }
    public State? Compensating { get; private set; }
    public State? Cancelled { get; private set; }

    public Event<OrderSubmitted>? OrderSubmittedEvent { get; private set; }
    public Event<PaymentApproved>? PaymentApprovedEvent { get; private set; }
    public Event<PaymentFailed>? PaymentFailedEvent { get; private set; }
    public Event<InventoryReserved>? InventoryReservedEvent { get; private set; }
    public Event<StockUnavailable>? StockUnavailableEvent { get; private set; }
    public Event<PaymentRefunded>? PaymentRefundedEvent { get; private set; }

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmittedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentApprovedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockUnavailableEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefundedEvent, x => x.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(OrderSubmittedEvent)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.OrderId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.TotalAmount = context.Message.TotalAmount;
                    context.Saga.CreatedAt = context.Message.Timestamp;
                    context.Saga.OrderItemsJson = System.Text.Json.JsonSerializer.Serialize(context.Message.Items);
                })
                .Send(context => new ProcessPayment(
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.TotalAmount,
                    DateTime.UtcNow))
                .TransitionTo(ProcessingPayment));

        During(ProcessingPayment,
            When(PaymentApprovedEvent)
                .Then(context =>
                {
                    context.Saga.TransactionId = context.Message.TransactionId;
                })
                .Send(context => new ReserveInventory(
                    context.Message.OrderId,
                    !string.IsNullOrEmpty(context.Saga.OrderItemsJson)
                        ? System.Text.Json.JsonSerializer.Deserialize<List<OrderItem>>(context.Saga.OrderItemsJson) ?? []
                        : [],
                    DateTime.UtcNow))
                .TransitionTo(ReservingInventory),
            When(PaymentFailedEvent)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                })
                .Publish(context => new OrderCancelled(
                    context.Message.OrderId,
                    $"Payment failed: {context.Message.Reason}",
                    DateTime.UtcNow))
                .Finalize());

        During(ReservingInventory,
            When(InventoryReservedEvent)
                .Then(context =>
                {
                    context.Saga.ReservationId = context.Message.ReservationId;
                    context.Saga.CompletedAt = DateTime.UtcNow;
                })
                .Publish(context => new OrderCompleted(
                    context.Message.OrderId,
                    DateTime.UtcNow))
                .TransitionTo(Completed)
                .Finalize(),
            When(StockUnavailableEvent)
                .Then(context =>
                {
                    context.Saga.FailureReason = $"Stock unavailable for products: {string.Join(", ", context.Message.UnavailableProducts)}";
                })
                .TransitionTo(Compensating)
                .IfElse(
                    context => !string.IsNullOrEmpty(context.Saga.TransactionId),
                    compensate => compensate
                        .Send(context => new RefundPayment(
                            context.Message.OrderId,
                            context.Saga.TransactionId!,
                            context.Saga.TotalAmount,
                            "Stock unavailable - refunding payment",
                            DateTime.UtcNow)),
                    skipRefund => skipRefund
                        .Publish(context => new OrderCancelled(
                            context.Message.OrderId,
                            context.Saga.FailureReason ?? "Stock unavailable",
                            DateTime.UtcNow))
                        .Finalize()));

        During(Compensating,
            When(PaymentRefundedEvent)
                .Publish(context => new OrderCancelled(
                    context.Message.OrderId,
                    context.Saga.FailureReason ?? "Order cancelled due to compensating transaction",
                    DateTime.UtcNow))
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
