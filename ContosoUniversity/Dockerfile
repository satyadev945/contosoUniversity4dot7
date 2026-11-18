# Build context: repository root (ApmOnonGd)
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining sources, build and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC \
    webpages__Enabled=false

# Database connection is supplied at runtime (SCHOOL_DB_CONNECTION or ConnectionStrings__SchoolContext)
EXPOSE 8080

# Non-root user (built into the official .NET 8 images)
USER $APP_UID

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
