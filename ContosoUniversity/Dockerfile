# ASP.NET Core (net8.0) multi-stage build.
# Build context is the REPOSITORY ROOT:
#   docker build -f ContosoUniversity/Dockerfile -t <image>:<tag> .
# NOTE: The requested base image mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 is a Windows/.NET Framework
# image and cannot run this net8.0 application, so the matching Linux aspnet:8.0 runtime image is used.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet restore layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-restore --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC \
    LANG=en_US.UTF-8 \
    webpages__Enabled=false

COPY --from=builder /app/publish .

# Non-root user (built into the official .NET 8 images, UID 1654)
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
