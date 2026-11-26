# syntax=docker/dockerfile:1
# Build context MUST be the repository root (Mono-db):
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build/publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=0 \
    DOTNET_GCHeapHardLimitPercent=75 \
    TZ=UTC \
    LANG=C.UTF-8

COPY --from=builder /app/publish .

# Built-in non-root 'app' user (UID 1654) shipped with .NET 8 images
USER 1654

EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
