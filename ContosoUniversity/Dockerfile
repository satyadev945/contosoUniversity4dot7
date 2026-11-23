# syntax=docker/dockerfile:1
# Build context MUST be the repository root:
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
# NOTE: EXPLICIT_BASE_IMAGE (mcr.microsoft.com/dotnet/framework/aspnet:4.7.2) cannot host a net8.0 app,
# so the matching Linux ASP.NET Core 8.0 runtime image is used.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet restore layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
    && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build \
       -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC \
    LANG=en_US.UTF-8 \
    AppSettings__webpages__Enabled=false
# Supply at runtime: ConnectionStrings__SchoolContext (or DB_HOST/DB_NAME/DB_USER/DB_PASSWORD)

COPY --from=builder /app/publish .

# Non-root user (built into the official .NET 8 images, UID 1654)
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
