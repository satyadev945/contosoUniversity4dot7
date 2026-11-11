# syntax=docker/dockerfile:1
# Build context: repository root (the directory containing ContosoUniversity.sln)
# Build with: docker build -f ContosoUniversity/Dockerfile -t <registry>/<repo>:<tag> .
#
# NOTE: The requested EXPLICIT_BASE_IMAGE (mcr.microsoft.com/dotnet/framework/runtime:4.7.2) is a
# Windows / .NET Framework console runtime and cannot host this net8.0 ASP.NET Core application
# nor run on Linux EKS nodes. The matching ASP.NET Core 8.0 runtime image is used instead.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Restore first (cached layer until the .csproj changes)
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy sources, then build and publish
COPY ContosoUniversity/ ContosoUniversity/
WORKDIR /src/ContosoUniversity
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-restore --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=0 \
    DOTNET_GCHeapHardLimitPercent=75 \
    TZ=UTC \
    LANG=en_US.UTF-8

COPY --from=builder /app/publish .

EXPOSE 8080

# Non-root user (built into the .NET 8 images, UID 1654)
USER $APP_UID

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
