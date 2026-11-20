# syntax=docker/dockerfile:1
# Build context: repository root.
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet restore layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining sources and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
    && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-restore --no-build /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_GCHeapAffinitizeMask= \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC

COPY --from=builder /app/publish .

EXPOSE 8080

# Built-in non-root user of the official .NET 8 images (UID 1654)
USER 1654

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
