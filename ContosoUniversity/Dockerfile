# Build context: repository root (docker build -f ContosoUniversity/Dockerfile .)
# NOTE: requested base image mcr.microsoft.com/dotnet/framework/runtime:4.7.2 is a Windows
# .NET Framework image and cannot run this net8.0 ASP.NET Core app, so aspnet:8.0 is used.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first for NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy source and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
    && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC

# Supply at runtime: ConnectionStrings__SchoolContext
EXPOSE 8080

# Non-root user (built into Microsoft .NET 8 images)
USER 1654

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
