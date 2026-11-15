# BASE_REGISTRY defaults to Microsoft Container Registry. In AWS, set it to the ECR pull-through cache
# prefix (e.g. <account>.dkr.ecr.<region>.amazonaws.com/mcr) so base images are served from ECR.
ARG BASE_REGISTRY=mcr.microsoft.com

# ---------- Build stage ----------
FROM ${BASE_REGISTRY}/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy only the project file first so the NuGet restore layer is cached
COPY ContosoUniversity.csproj .
RUN dotnet restore ContosoUniversity.csproj

# Copy the rest of the source, build and publish
COPY . .
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-build \
    && find /app/publish -type f \( -name "*.pdb" -o -name "*.tmp" \) -delete

# ---------- Runtime stage ----------
FROM ${BASE_REGISTRY}/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=0 \
    TZ=UTC

EXPOSE 8080

# Run as the built-in non-root "app" user (UID 1654) shipped with the .NET 8 images
USER app

# Exec-form entrypoint so dotnet receives SIGTERM directly (graceful shutdown on ECS stop)
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
