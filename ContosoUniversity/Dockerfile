# syntax=docker/dockerfile:1
# Build context: repository root (docker build -f ContosoUniversity/Dockerfile .)
# NOTE: the project targets net8.0 (ASP.NET Core, Linux). The requested Windows image
# mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 cannot run a net8.0 app, so the matching
# mcr.microsoft.com/dotnet/aspnet:8.0 runtime is used.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Restore first for NuGet layer caching
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore \
    && dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-restore --no-build \
       -p:DebugType=None -p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC \
    webpages__Enabled=false
# Database connection is supplied at runtime via ConnectionStrings__SchoolContext

EXPOSE 8080

# Non-root user (built into the .NET 8 images)
USER app
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
