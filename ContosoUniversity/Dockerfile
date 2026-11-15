# Build context MUST be the repository root (the folder containing ContosoUniversity.sln):
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build / publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC

COPY --from=builder --chown=app:app /app/publish .

# Non-root user (UID 1654 = "app" in the official .NET 8 images; numeric so Kubernetes runAsNonRoot can verify it)
USER 1654

EXPOSE 8080

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
