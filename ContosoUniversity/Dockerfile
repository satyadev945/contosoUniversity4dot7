# Build context: repository root (run: docker build -f ContosoUniversity/Dockerfile .)
# NOTE: The requested explicit base image (mcr.microsoft.com/dotnet/framework/runtime:4.7.2) is a
# Windows/.NET Framework image and cannot run this net8.0 ASP.NET Core application, so the
# matching official ASP.NET Core 8.0 runtime image is used instead.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file first for NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

# Copy the remaining source
COPY ContosoUniversity/ ContosoUniversity/
WORKDIR /src/ContosoUniversity
RUN dotnet build ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity.csproj -c Release -o /app/publish --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=builder --chown=1654:1654 /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    TZ=UTC

# Supply at runtime: ConnectionStrings__SchoolContext
EXPOSE 8080

# Non-root user (built into the aspnet image, UID 1654)
USER 1654

ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
