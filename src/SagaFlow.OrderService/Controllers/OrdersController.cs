using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SagaFlow.Contracts.Events;

namespace SagaFlow.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IPublishEndpoint publishEndpoint) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderRequest request)
    {
        var orderId = Guid.NewGuid();
        
        var orderSubmitted = new OrderSubmitted(
            orderId,
            request.CustomerId,
            request.TotalAmount,
            request.Items.Select(i => new OrderItem(i.ProductId, i.Quantity, i.Price)).ToList(),
            DateTime.UtcNow);

        await publishEndpoint.Publish(orderSubmitted);

        return Accepted(new { OrderId = orderId, Message = "Order submitted successfully" });
    }
}

public record SubmitOrderRequest(
    string CustomerId,
    decimal TotalAmount,
    List<OrderItemRequest> Items);

public record OrderItemRequest(
    string ProductId,
    int Quantity,
    decimal Price);
