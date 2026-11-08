# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/docker-restore.sh", "backend/docker-restore.sh"]
COPY ["backend/ApprovalFlow.PolicyRag/ApprovalFlow.PolicyRag.csproj", "backend/ApprovalFlow.PolicyRag/"]
COPY ["backend/ApprovalFlow.Observability/ApprovalFlow.Observability.csproj", "backend/ApprovalFlow.Observability/"]
COPY ["backend/ApprovalFlow.Reliability/ApprovalFlow.Reliability.csproj", "backend/ApprovalFlow.Reliability/"]
COPY ["backend/ApprovalService/ApprovalService.csproj", "backend/ApprovalService/"]
RUN sed -i 's/\r$//' backend/docker-restore.sh && chmod +x backend/docker-restore.sh
RUN --mount=type=cache,target=/root/.nuget/packages \
    ./backend/docker-restore.sh "backend/ApprovalService/ApprovalService.csproj"
COPY ["backend/ApprovalFlow.PolicyRag/", "backend/ApprovalFlow.PolicyRag/"]
COPY ["backend/ApprovalFlow.Observability/", "backend/ApprovalFlow.Observability/"]
COPY ["backend/ApprovalFlow.Reliability/", "backend/ApprovalFlow.Reliability/"]
COPY ["backend/ApprovalService/", "backend/ApprovalService/"]
WORKDIR "/src/backend/ApprovalService"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "ApprovalService.csproj" \
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
ENTRYPOINT ["dotnet", "ApprovalService.dll"]
