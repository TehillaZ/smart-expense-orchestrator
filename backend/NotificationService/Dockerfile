# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/ApprovalFlow.Observability/ApprovalFlow.Observability.csproj", "backend/ApprovalFlow.Observability/"]
COPY ["backend/NotificationService/NotificationService.csproj", "backend/NotificationService/"]
RUN dotnet restore "backend/NotificationService/NotificationService.csproj"
COPY ["backend/ApprovalFlow.Observability/", "backend/ApprovalFlow.Observability/"]
COPY ["backend/NotificationService/", "backend/NotificationService/"]
WORKDIR "/src/backend/NotificationService"
RUN dotnet publish "NotificationService.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
