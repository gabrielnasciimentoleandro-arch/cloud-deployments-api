# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props ./
COPY src/CloudDeploy.Api/CloudDeploy.Api.csproj src/CloudDeploy.Api/
COPY src/CloudDeploy.Api/packages.lock.json src/CloudDeploy.Api/
RUN dotnet restore src/CloudDeploy.Api/CloudDeploy.Api.csproj --locked-mode

COPY src/CloudDeploy.Api/ src/CloudDeploy.Api/
RUN dotnet publish src/CloudDeploy.Api/CloudDeploy.Api.csproj \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
COPY --from=build /app/publish .

USER 1654

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD ["wget", "-q", "-O", "/dev/null", "http://127.0.0.1:8080/healthz"]

ENTRYPOINT ["dotnet", "CloudDeploy.Api.dll"]
