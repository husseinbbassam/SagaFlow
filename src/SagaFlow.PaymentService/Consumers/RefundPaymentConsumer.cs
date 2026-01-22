using MassTransit;
using SagaFlow.Contracts.Commands;
using SagaFlow.Contracts.Events;

namespace SagaFlow.PaymentService.Consumers;

public class RefundPaymentConsumer(ILogger<RefundPaymentConsumer> logger) : IConsumer<RefundPayment>
{
    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        logger.LogInformation("Processing refund for Order {OrderId}, Transaction: {TransactionId}, Amount: {Amount}", 
            context.Message.OrderId, 
            context.Message.TransactionId,
            context.Message.Amount);

        // Simulate refund processing delay
        await Task.Delay(TimeSpan.FromSeconds(1));

        logger.LogInformation("Refund completed for Order {OrderId}, Transaction: {TransactionId}", 
            context.Message.OrderId, 
            context.Message.TransactionId);

        await context.Publish(new PaymentRefunded(
            context.Message.OrderId,
            context.Message.TransactionId,
            context.Message.Amount,
            DateTime.UtcNow));
    }
}
