# Build context: repository root (ApSdbzMono)
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
#
# NOTE: The application targets net8.0 (ASP.NET Core). The requested explicit base image
# (mcr.microsoft.com/dotnet/framework/aspnet:4.7.2) is a Windows / .NET Framework image that
# cannot host a net8.0 application, so the matching Linux aspnet:8.0 runtime image is used.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build
COPY ContosoUniversity/ ContosoUniversity/
WORKDIR /src/ContosoUniversity
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC

COPY --from=builder /app/publish .

# Database connection is supplied at runtime via SCHOOL_DB_CONNECTION
# (or ConnectionStrings__SchoolContext).
EXPOSE 8080

# Non-root user (APP_UID is provided by the official .NET 8 images, uid 1654)
USER $APP_UID
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
