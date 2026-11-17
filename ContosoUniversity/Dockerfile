# syntax=docker/dockerfile:1
# Build context: repository root (AppDBMono_CS1). Build with:
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
#
# NOTE: The requested EXPLICIT_BASE_IMAGE (mcr.microsoft.com/dotnet/framework/runtime:4.7.2) is a Windows
# .NET Framework 4.7.2 image and cannot run this application, which targets net8.0 (ASP.NET Core).
# The matching ASP.NET Core 8.0 runtime image is used instead.

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
 && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build \
    -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_TieredPGO=1 \
    DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC \
    ConnectionStrings__SchoolContext=""

# Non-root user (built into Microsoft .NET 8 images; UID 1654)
USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
