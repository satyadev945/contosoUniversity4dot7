# syntax=docker/dockerfile:1
# Build context MUST be the repository root:
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build
COPY ContosoUniversity/ ContosoUniversity/
WORKDIR /src/ContosoUniversity
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-build \
    -p:DebugType=None -p:DebugSymbols=false -p:UseAppHost=false

# ---------- Runtime stage ----------
# NOTE: the application targets net8.0 (ASP.NET Core). The requested explicit base image
# mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 only runs .NET Framework 4.x apps and
# cannot host a net8.0 application, so the matching ASP.NET Core 8.0 runtime image is used.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=0 \
    TZ=UTC

COPY --from=builder /app/publish .

# Database connection is supplied at runtime, e.g. ConnectionStrings__SchoolContext
EXPOSE 8080

# Built-in non-root user of the official ASP.NET Core 8 image
USER app

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
