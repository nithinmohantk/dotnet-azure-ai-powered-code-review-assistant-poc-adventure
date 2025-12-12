# Multi-stage Dockerfile for AI-Powered Code Review Assistant
# Optimized for production with security hardening

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install security scanning tools
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy csproj and restore as distinct layers
COPY src/Core/Domain/CodeReviewAssistant.Core.Domain/CodeReviewAssistant.Core.Domain.csproj ./Core/Domain/CodeReviewAssistant.Core.Domain/
COPY src/Core/Application/CodeReviewAssistant.Core.Application/CodeReviewAssistant.Core.Application.csproj ./Core/Application/CodeReviewAssistant.Core.Application/
COPY src/Infrastructure/ExternalServices/CodeReviewAssistant.Infrastructure.ExternalServices/CodeReviewAssistant.Infrastructure.ExternalServices.csproj ./Infrastructure/ExternalServices/CodeReviewAssistant.Infrastructure.ExternalServices/
COPY src/Infrastructure/Persistence/CodeReviewAssistant.Infrastructure.Persistence/CodeReviewAssistant.Infrastructure.Persistence.csproj ./Infrastructure/Persistence/CodeReviewAssistant.Infrastructure.Persistence/
COPY src/WebApi/CodeReviewAssistant.WebApi/CodeReviewAssistant.WebApi.csproj ./WebApi/CodeReviewAssistant.WebApi/
COPY src/Shared/CodeReviewAssistant.Shared/CodeReviewAssistant.Shared.csproj ./Shared/CodeReviewAssistant.Shared/
COPY tests/Unit/CodeReviewAssistant.Unit.Tests/CodeReviewAssistant.Unit.Tests.csproj ./tests/Unit/CodeReviewAssistant.Unit.Tests/
COPY tests/Integration/CodeReviewAssistant.Integration.Tests/CodeReviewAssistant.Integration.Tests.csproj ./tests/Integration/CodeReviewAssistant.Integration.Tests/

# Restore dependencies
RUN dotnet restore "WebApi/CodeReviewAssistant.WebApi/CodeReviewAssistant.WebApi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/WebApi/CodeReviewAssistant.WebApi"
RUN dotnet build "CodeReviewAssistant.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeReviewAssistant.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage with security hardening
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Install security tools and hardening
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set secure permissions
RUN chown -R appuser:appuser /app
RUN chmod -R 755 /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables for security
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "CodeReviewAssistant.WebApi.dll"]
