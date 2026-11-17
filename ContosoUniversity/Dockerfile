# Build context: repository root (docker build -f ContosoUniversity/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first for NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build/publish
COPY ContosoUniversity/ ContosoUniversity/
WORKDIR /src/ContosoUniversity
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-restore --no-build /p:UseAppHost=false

# NOTE: The requested base image (mcr.microsoft.com/dotnet/framework/aspnet:4.7.2) is a Windows/.NET Framework
# image and cannot run this net8.0 application nor be scheduled on Linux GKE nodes.
# The matching ASP.NET Core 8.0 runtime image is used instead.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC

COPY --from=builder /app/publish .

EXPOSE 8080

# Non-root user provided by the official .NET 8 images (UID 1654)
USER 1654

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
