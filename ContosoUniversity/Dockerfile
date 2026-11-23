# Build context: repository root (the folder containing ContosoUniversity.sln)
#   docker build -f ContosoUniversity/Dockerfile -t contosouniversity:latest .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Restore first so NuGet packages are cached in their own layer
COPY ContosoUniversity/ContosoUniversity.csproj ContosoUniversity/
RUN dotnet restore ContosoUniversity/ContosoUniversity.csproj

COPY ContosoUniversity/ ContosoUniversity/
RUN dotnet build ContosoUniversity/ContosoUniversity.csproj -c Release --no-restore
RUN dotnet publish ContosoUniversity/ContosoUniversity.csproj -c Release -o /app/publish --no-build -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0 \
    DOTNET_TieredPGO=1 \
    TZ=UTC

COPY --from=builder --chown=app:app /app/publish .

# Non-root user ("app" is built into the .NET 8 images)
USER app

# Supply the database connection string at runtime:
#   ConnectionStrings__SchoolContext="Server=<host>;Database=<db>;User Id=<user>;Password=<pwd>;"
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoUniversity.dll"]
