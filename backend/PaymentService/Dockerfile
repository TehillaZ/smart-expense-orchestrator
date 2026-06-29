# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/PaymentService/PaymentService.csproj", "backend/PaymentService/"]
RUN dotnet restore "backend/PaymentService/PaymentService.csproj"
COPY ["backend/PaymentService/", "backend/PaymentService/"]
WORKDIR "/src/backend/PaymentService"
RUN dotnet publish "PaymentService.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM runtime AS final
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
