# Build context: the repository root (AppCsharpMono)
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
#
# NOTE: the application targets net8.0 (ASP.NET Core, Kestrel) so it cannot run on the
# Windows-only .NET Framework 4.7.2 image; the Linux ASP.NET Core 8.0 runtime image is used.

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first so NuGet restore is cached as its own layer
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build
COPY ContosoUniversity/ ContosoUniversity/
WORKDIR /src/ContosoUniversity
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC

# Supply at runtime, e.g.
# ConnectionStrings__SchoolContext="Server=<host>;Database=ContosoUniversity;User Id=<user>;Password=<password>;TrustServerCertificate=True"
EXPOSE 8080

# Non-root user (built into the official .NET 8 images, UID 1654)
USER app

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
