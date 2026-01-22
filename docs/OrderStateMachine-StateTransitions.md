# OrderStateMachine State Transitions

## Overview

The `OrderStateMachine` orchestrates a distributed transaction across Order, Payment, and Inventory services using the **Saga Pattern**. It manages state transitions based on events and coordinates compensating transactions when failures occur.

## State Diagram

```
Initial (null)
    |
    | OrderSubmittedEvent
    ↓
ProcessingPayment
    |
    ├─→ PaymentApprovedEvent → ReservingInventory
    |                               |
    |                               ├─→ InventoryReservedEvent → Completed (Finalized)
    |                               |
    |                               └─→ StockUnavailableEvent → Compensating
    |                                                               |
    |                                                               └─→ PaymentRefundedEvent → Cancelled (Finalized)
    |
    └─→ PaymentFailedEvent → Cancelled (Finalized)
```

## States

### 1. **Initial State** (null)
- **Description**: The saga instance does not exist yet
- **Entry**: When a new order is about to be submitted
- **Exit**: After `OrderSubmittedEvent` is received

### 2. **ProcessingPayment**
- **Description**: Payment is being processed by the Payment Service
- **Entry**: Immediately after `OrderSubmittedEvent` is received
- **Actions on Entry**:
  - Store order details (CorrelationId, CustomerId, TotalAmount, OrderItems)
  - Send `ProcessPayment` command to Payment Service
- **Possible Transitions**:
  - `PaymentApprovedEvent` → Transition to **ReservingInventory**
  - `PaymentFailedEvent` → Transition to **Cancelled** (Finalized)

### 3. **ReservingInventory**
- **Description**: Inventory is being reserved by the Inventory Service
- **Entry**: After payment is successfully approved
- **Actions on Entry**:
  - Store transaction ID from payment
  - Send `ReserveInventory` command to Inventory Service
- **Possible Transitions**:
  - `InventoryReservedEvent` → Transition to **Completed** (Finalized)
  - `StockUnavailableEvent` → Transition to **Compensating**

### 4. **Completed**
- **Description**: Order successfully completed (happy path)
- **Entry**: After inventory is successfully reserved
- **Actions on Entry**:
  - Store reservation ID
  - Set completion timestamp
  - Publish `OrderCompleted` event
  - **Finalize** the saga (marks for deletion)

### 5. **Compensating**
- **Description**: Compensating transaction in progress (rolling back)
- **Entry**: When stock is unavailable after payment was approved
- **Actions on Entry**:
  - Store failure reason with unavailable products
- **Compensation Logic**:
  - **If TransactionId exists** (payment was processed):
    - Send `RefundPayment` command to Payment Service
    - Wait for `PaymentRefundedEvent`
  - **If no TransactionId** (unlikely edge case):
    - Publish `OrderCancelled` event immediately
    - **Finalize** the saga

### 6. **Cancelled** (Finalized)
- **Description**: Order cancelled due to failure
- **Entry**: Either from payment failure or after successful refund
- **Actions on Entry**:
  - Publish `OrderCancelled` event with failure reason
  - **Finalize** the saga (marks for deletion)

## StockUnavailable Failure Handling (Detailed)

The `StockUnavailableEvent` represents a critical failure point that requires **compensating transactions** to maintain data consistency.

### Scenario: Stock Unavailable After Payment Success

1. **Context**:
   - Current State: `ReservingInventory`
   - Payment was already approved and charged
   - Inventory check discovers insufficient stock

2. **Event Received**: `StockUnavailableEvent`
   - Contains: OrderId, List of UnavailableProducts, Timestamp

3. **State Transition**: `ReservingInventory` → `Compensating`

4. **Compensation Decision** (using `IfElse`):

   ```csharp
   .IfElse(
       context => !string.IsNullOrEmpty(context.Saga.TransactionId),
       compensate => compensate
           .Send(context => new RefundPayment(...)),
       skipRefund => skipRefund
           .Publish(context => new OrderCancelled(...))
           .Finalize()
   )
   ```

   **Condition**: Check if `TransactionId` exists (payment was processed)

   #### Path A: TransactionId Exists (Normal Case)
   - **Action**: Send `RefundPayment` command
     - OrderId: Current order
     - TransactionId: Stored from payment approval
     - Amount: Original total amount
     - Reason: "Stock unavailable - refunding payment"
   - **Wait**: Stay in `Compensating` state until `PaymentRefundedEvent` arrives
   - **On PaymentRefundedEvent**:
     - Publish `OrderCancelled` event
     - **Finalize** saga

   #### Path B: No TransactionId (Edge Case)
   - **Scenario**: Extremely rare - received StockUnavailable before PaymentApproved
   - **Action**: Publish `OrderCancelled` immediately
   - **Finalize** saga without refund

5. **Final State**: `Cancelled` (Finalized)

### Key Design Decisions

#### Why Compensate Instead of Retry?
- **Inventory is a finite resource**: Retrying won't create more stock
- **Customer experience**: Better to cancel quickly than wait indefinitely
- **Eventual consistency**: Allows system to remain available during failures

#### Why Check TransactionId?
- **Safety**: Prevents sending refund commands without a valid transaction
- **Idempotency**: Ensures we only refund if payment was actually processed
- **State consistency**: Handles race conditions gracefully

#### Why Publish OrderCancelled in Compensating State?
- **Notification**: Informs other services and clients about cancellation
- **Audit trail**: Creates a record of the compensation
- **Decoupling**: Other services can react to cancellation without knowing saga internals

## Event Correlation

All events are correlated by `OrderId` (CorrelationId):

```csharp
Event(() => StockUnavailableEvent, 
    x => x.CorrelateById(context => context.Message.OrderId));
```

This ensures events are routed to the correct saga instance.

## State Persistence

The saga state is persisted in PostgreSQL using Entity Framework Core:
- **Table**: `saga.OrderStates`
- **Primary Key**: `CorrelationId` (OrderId)
- **State Column**: `CurrentState` (stores current state name as string)

This enables:
- **Durability**: Survives service restarts
- **Debuggability**: Can query current state of any order
- **Recovery**: Can resume from last known state after crashes

## Message Delivery Guarantees

### At-Least-Once Delivery
- Messages may be redelivered on failure
- All handlers must be **idempotent**

### Exactly-Once Processing
- MassTransit + EF Core provides **inbox pattern**
- Duplicate messages are detected and skipped

### Retry Policy
From infrastructure configuration:
- **Exponential backoff**: 5 retries (2s, 4s, 8s, 16s, 30s)
- **Delayed redelivery**: 3 retries (5s, 30s, 60s)
- Ensures transient failures are handled gracefully

## Testing the StockUnavailable Flow

### Manual Test Steps

1. **Submit Order**:
   ```bash
   curl -X POST http://localhost:5000/api/orders \
     -H "Content-Type: application/json" \
     -d '{
       "customerId": "CUST-001",
       "totalAmount": 199.99,
       "items": [{"productId": "PROD-123", "quantity": 2, "price": 99.99}]
     }'
   ```

2. **Observe Logs**:
   - OrderService: "Order submitted"
   - PaymentService: "Payment approved" (90% chance)
   - InventoryService: "Stock unavailable" (20% chance after payment success)
   - PaymentService: "Refund completed"
   - OrderService: "Order cancelled"

3. **Query Database**:
   ```sql
   SELECT "CorrelationId", "CurrentState", "FailureReason", "TransactionId", "ReservationId"
   FROM saga."OrderStates"
   WHERE "CorrelationId" = '<your-order-id>';
   ```

   **Expected Result** (for StockUnavailable scenario):
   - CurrentState: NULL (finalized)
   - FailureReason: "Stock unavailable for products: ..."
   - TransactionId: "TXN-..." (exists)
   - ReservationId: NULL (never reserved)

### Expected Message Flow (Stock Unavailable)

```
1. OrderSubmitted       (API → Saga)
2. ProcessPayment       (Saga → PaymentService)
3. PaymentApproved      (PaymentService → Saga)
4. ReserveInventory     (Saga → InventoryService)
5. StockUnavailable     (InventoryService → Saga)
6. RefundPayment        (Saga → PaymentService)
7. PaymentRefunded      (PaymentService → Saga)
8. OrderCancelled       (Saga → Published)
```

## Summary

The `OrderStateMachine` handles the `StockUnavailable` failure by:

1. **Detecting** the failure in the `ReservingInventory` state
2. **Storing** the failure reason for audit purposes
3. **Transitioning** to the `Compensating` state
4. **Conditionally sending** a `RefundPayment` command if payment was processed
5. **Waiting** for the refund to complete (`PaymentRefundedEvent`)
6. **Publishing** an `OrderCancelled` event to notify the system
7. **Finalizing** the saga to mark it as complete (soft delete)

This ensures **data consistency** across distributed services while maintaining **system availability** during failures.
