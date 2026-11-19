# syntax=docker/dockerfile:1
# Build context: repository root (the folder containing ContosoUniversity.sln)
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first to maximise NuGet restore layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and build/publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore /p:UseAppHost=false
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-restore --no-build /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC

COPY --from=builder --chown=app:app /app/publish .

# Non-root user provided by the official .NET 8 images ("app", UID 1654)
USER app

EXPOSE 8080

# Database connection is supplied at runtime:
#   -e ConnectionStrings__SchoolContext="Server=...;Database=...;User Id=...;Password=...;"
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
