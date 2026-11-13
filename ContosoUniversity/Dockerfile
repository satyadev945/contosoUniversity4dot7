# syntax=docker/dockerfile:1
# Build context: repository root  (docker build -f ContosoUniversity/Dockerfile .)

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file first to cache NuGet restore layer
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source and publish
COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
    && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

# ---------- Runtime stage ----------
# NOTE: the requested explicit image (mcr.microsoft.com/dotnet/framework/runtime:4.7.2) is a Windows
# .NET Framework image and cannot run this net8.0 ASP.NET Core application, so the matching
# Linux ASP.NET Core 8.0 runtime image is used instead.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC

EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root user shipped with the .NET 8 images
USER $APP_UID

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
