# Docker Compose Setup for SagaFlow

This docker-compose.yml orchestrates all services required for the SagaFlow distributed saga pattern demonstration.

## Services

### Infrastructure Services

#### PostgreSQL
- **Image**: postgres:16-alpine
- **Container**: sagaflow-postgres
- **Port**: 5432
- **Database**: sagaflow
- **Credentials**: postgres/postgres
- **Health Check**: pg_isready every 10 seconds
- **Purpose**: Stores saga state persistence for OrderService

#### RabbitMQ
- **Image**: rabbitmq:3-management-alpine
- **Container**: sagaflow-rabbitmq
- **Ports**: 
  - 5672 (AMQP protocol)
  - 15672 (Management UI)
- **Credentials**: guest/guest
- **Health Check**: rabbitmq-diagnostics ping every 10 seconds
- **Purpose**: Message broker for event-driven communication
- **Management UI**: http://localhost:15672

### Application Services

#### OrderService
- **Container**: sagaflow-orderservice
- **Port**: 8080
- **Type**: Web API (.NET 9)
- **Purpose**: Hosts the OrderStateMachine saga orchestrator
- **Dependencies**: PostgreSQL (saga persistence), RabbitMQ (messaging)
- **Health Check**: HTTP GET /health every 30 seconds
- **API Endpoint**: http://localhost:8080

#### PaymentService
- **Container**: sagaflow-paymentservice
- **Type**: Worker Service (.NET 9)
- **Purpose**: Processes payment commands and publishes payment events
- **Dependencies**: RabbitMQ (messaging)
- **Behavior**: 
  - Consumes `ProcessPayment` commands
  - Publishes `PaymentApproved` or `PaymentFailed` events
  - Consumes `RefundPayment` commands
  - Publishes `PaymentRefunded` events
  - Simulates 90% success rate using Random class

#### InventoryService
- **Container**: sagaflow-inventoryservice
- **Type**: Worker Service (.NET 9)
- **Purpose**: Manages inventory reservations
- **Dependencies**: RabbitMQ (messaging)
- **Behavior**:
  - Consumes `ReserveInventory` commands
  - Publishes `InventoryReserved` or `StockUnavailable` events
  - Simulates 80% success rate using Random class

## Network Configuration

All services are connected via a bridge network named `sagaflow-network`, enabling:
- Service discovery by container name
- Isolated communication
- Secure inter-service messaging

### Service Communication

Services use the following hostnames within the Docker network:
- **PostgreSQL**: `postgres:5432`
- **RabbitMQ**: `rabbitmq:5672`
- **OrderService**: `orderservice:8080`

Environment variables configure each service to use these internal hostnames.

## Environment Variables

### OrderService
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__SagaDb=Host=postgres;Port=5432;Database=sagaflow;Username=postgres;Password=postgres`
- `RabbitMq__Host=rabbitmq`
- `RabbitMq__Username=guest`
- `RabbitMq__Password=guest`

### PaymentService & InventoryService
- `DOTNET_ENVIRONMENT=Production`
- `RabbitMq__Host=rabbitmq`
- `RabbitMq__Username=guest`
- `RabbitMq__Password=guest`

## Usage

### Starting All Services

```bash
docker-compose up -d
```

This will:
1. Pull/build all required images
2. Start PostgreSQL and RabbitMQ
3. Wait for health checks to pass
4. Build and start the .NET services
5. Apply database migrations automatically

### Viewing Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f orderservice
docker-compose logs -f paymentservice
docker-compose logs -f inventoryservice
```

### Stopping Services

```bash
docker-compose down
```

### Stopping and Removing Volumes

```bash
docker-compose down -v
```

## Testing the System

### 1. Verify Services are Running

```bash
docker-compose ps
```

All services should show as "Up" and healthy.

### 2. Check RabbitMQ Management UI

Open http://localhost:15672 (guest/guest)

- Verify queues are created
- Monitor message flow

### 3. Submit a Test Order

```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-001",
    "totalAmount": 199.99,
    "items": [
      {
        "productId": "PROD-123",
        "quantity": 2,
        "price": 99.99
      }
    ]
  }'
```

### 4. Observe the Saga Flow

Watch the logs to see the saga orchestration:

```bash
docker-compose logs -f orderservice paymentservice inventoryservice
```

Expected flow:
1. OrderService receives order and creates saga
2. PaymentService processes payment (90% success rate)
3. InventoryService reserves stock (80% success rate)
4. On success: Order completed
5. On stock failure: Payment refunded (compensation)

### 5. Check Saga State in Database

```bash
docker-compose exec postgres psql -U postgres -d sagaflow

# Query saga states
SELECT "CorrelationId", "CurrentState", "FailureReason", "TransactionId", "ReservationId"
FROM saga."OrderStates"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

## Health Checks

### OrderService Health
```bash
curl http://localhost:8080/health
```

### RabbitMQ Health
```bash
docker-compose exec rabbitmq rabbitmq-diagnostics ping
```

### PostgreSQL Health
```bash
docker-compose exec postgres pg_isready -U postgres
```

## Troubleshooting

### Services Not Starting

Check logs:
```bash
docker-compose logs <service-name>
```

### Connection Issues

Verify network:
```bash
docker network inspect sagaflow_sagaflow-network
```

### Database Migration Issues

Manually run migrations:
```bash
docker-compose exec orderservice dotnet ef database update
```

### RabbitMQ Connection Refused

Ensure RabbitMQ is healthy:
```bash
docker-compose ps rabbitmq
```

Wait for health check to pass before starting application services.

## Volumes

- **postgres-data**: Persists PostgreSQL database data across container restarts

## Scaling Services

Payment and Inventory services can be scaled:

```bash
docker-compose up -d --scale paymentservice=3 --scale inventoryservice=2
```

MassTransit will automatically distribute messages across instances.

## Building Custom Images

To rebuild images after code changes:

```bash
docker-compose build
docker-compose up -d
```

Or rebuild specific service:

```bash
docker-compose build orderservice
docker-compose up -d orderservice
```

## Production Considerations

For production deployment, consider:

1. **Security**:
   - Use secrets for credentials
   - Enable TLS for RabbitMQ
   - Use SSL for PostgreSQL connections
   - Implement authentication/authorization

2. **Scalability**:
   - Use managed PostgreSQL (e.g., AWS RDS, Azure Database)
   - Use managed RabbitMQ (e.g., CloudAMQP)
   - Scale services independently based on load

3. **Monitoring**:
   - Add logging aggregation (e.g., ELK stack)
   - Implement distributed tracing (e.g., Jaeger, Zipkin)
   - Use metrics collection (e.g., Prometheus, Grafana)

4. **Resilience**:
   - Configure retry policies
   - Implement circuit breakers
   - Use message persistence
   - Set up backup and disaster recovery

## License

MIT License - See LICENSE file in repository root.
