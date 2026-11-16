# Build context: repository root (the folder containing ContosoUniversity.sln)
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
# NOTE: The requested base image mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 is a Windows
# .NET Framework image and cannot run this net8.0 ASP.NET Core application, so the official
# Linux ASP.NET Core 8.0 runtime image is used instead.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Restore first for NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the sources, build and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release --no-build -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    DOTNET_gcServer=0 \
    TZ=UTC

# External services are supplied at runtime:
#   ConnectionStrings__SchoolContext  or  DB_HOST / DB_NAME / DB_USER / DB_PASSWORD
#   APPLICATIONINSIGHTS_CONNECTION_STRING (optional)
EXPOSE 8080

# Non-root user (APP_UID is predefined in the official .NET 8 images)
USER $APP_UID
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
