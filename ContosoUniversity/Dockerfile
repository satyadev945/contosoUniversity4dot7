# Build context: repository root (directory containing ContosoUniversity.sln)
# docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
#
# NOTE: the application targets net8.0 (ASP.NET Core). The requested base image
# mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 is a Windows/.NET Framework image that cannot
# run net8.0 assemblies, so the matching Linux runtime image is used instead.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to cache NuGet restore layer
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy source and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
    && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

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

# Non-root user (built into .NET 8 images, UID 1654)
USER $APP_UID

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
