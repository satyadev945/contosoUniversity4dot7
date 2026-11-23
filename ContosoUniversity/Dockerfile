# Build context = repository root (docker build -f ContosoUniversity/Dockerfile .)
# The application targets net8.0 (ASP.NET Core, Linux). The requested explicit base image
# (mcr.microsoft.com/dotnet/framework/aspnet:4.7.2) is a Windows/.NET Framework image that cannot host net8.0.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first for NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build/publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC

# Supply at runtime: ConnectionStrings__SchoolContext, or DB_HOST/DB_PORT/DB_NAME/DB_USER/DB_PASSWORD
EXPOSE 8080

# Non-root user (APP_UID is defined by the official .NET 8 images)
USER $APP_UID
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
