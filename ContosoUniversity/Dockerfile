# Build from the repository root (the docker build context must be "."):
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the rest of the sources, then build and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore /p:UseAppHost=false
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release --no-build -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC

EXPOSE 8080

# Non-root user (built into the official .NET 8 images)
USER $APP_UID

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
