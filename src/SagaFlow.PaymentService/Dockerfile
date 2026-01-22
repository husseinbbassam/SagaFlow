# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SagaFlow.sln", "./"]
COPY ["src/SagaFlow.PaymentService/SagaFlow.PaymentService.csproj", "src/SagaFlow.PaymentService/"]
COPY ["src/SagaFlow.Contracts/SagaFlow.Contracts.csproj", "src/SagaFlow.Contracts/"]
COPY ["src/SagaFlow.Infrastructure/SagaFlow.Infrastructure.csproj", "src/SagaFlow.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/SagaFlow.PaymentService/SagaFlow.PaymentService.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/SagaFlow.PaymentService"
RUN dotnet build "SagaFlow.PaymentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SagaFlow.PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SagaFlow.PaymentService.dll"]
