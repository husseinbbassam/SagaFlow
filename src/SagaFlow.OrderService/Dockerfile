# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SagaFlow.sln", "./"]
COPY ["src/SagaFlow.OrderService/SagaFlow.OrderService.csproj", "src/SagaFlow.OrderService/"]
COPY ["src/SagaFlow.Contracts/SagaFlow.Contracts.csproj", "src/SagaFlow.Contracts/"]
COPY ["src/SagaFlow.Infrastructure/SagaFlow.Infrastructure.csproj", "src/SagaFlow.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/SagaFlow.OrderService/SagaFlow.OrderService.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/SagaFlow.OrderService"
RUN dotnet build "SagaFlow.OrderService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SagaFlow.OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SagaFlow.OrderService.dll"]
