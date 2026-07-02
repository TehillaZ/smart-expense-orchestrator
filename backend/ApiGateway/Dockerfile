# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/ApiGateway/ApiGateway.csproj", "backend/ApiGateway/"]
RUN dotnet restore "backend/ApiGateway/ApiGateway.csproj"
COPY ["backend/ApiGateway/", "backend/ApiGateway/"]
WORKDIR "/src/backend/ApiGateway"
RUN dotnet publish "ApiGateway.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
